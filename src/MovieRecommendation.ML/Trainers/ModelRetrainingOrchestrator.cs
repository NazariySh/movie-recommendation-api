using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.DTOs.Recommendations;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Predictions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.ML.Models;
using MovieRecommendation.ML.Predictors;

namespace MovieRecommendation.ML.Trainers;

public class ModelRetrainingOrchestrator : IModelRetrainingOrchestrator
{
    public const int MinRatingsToTrain = 100;

    private const int MaxPredictionsPerUser = 200;
    private const int InsertBatchSize = 5000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModelRetrainingOrchestrator> _logger;

    public ModelRetrainingOrchestrator(
        IServiceScopeFactory scopeFactory,
        ILogger<ModelRetrainingOrchestrator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<RetrainResultDto> RunAsync(CancellationToken cancellationToken = default)
    {
        var jobId = NewJobId();
        var triggeredAt = DateTime.UtcNow;

        var result = await ExecuteAsync(jobId, cancellationToken);

        return new RetrainResultDto
        {
            JobId = jobId,
            Status = $"Completed: {result.Version} (RMSE {result.Rmse:F4})",
            TriggeredAt = triggeredAt,
        };
    }

    public RetrainResultDto Enqueue()
    {
        var jobId = NewJobId();
        var triggeredAt = DateTime.UtcNow;

        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteAsync(jobId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background retrain job {JobId} failed", jobId);
            }
        });

        return new RetrainResultDto
        {
            JobId = jobId,
            Status = "Started",
            TriggeredAt = triggeredAt,
        };
    }

    private async Task<TrainingResult> ExecuteAsync(string jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var trainer = sp.GetRequiredService<RecommendationModelTrainer>();
        var metadataRepo = sp.GetRequiredService<IMlModelMetadataRepository>();

        _logger.LogInformation("Retrain job {JobId} fetching ratings...", jobId);

        var ratings = await db.Ratings
            .AsNoTracking()
            .Select(r => new MovieRatingData
            {
                UserId = r.UserId.ToString(),
                MovieId = r.MovieId.ToString(),
                Label = (float)r.Score,
            })
            .ToListAsync(ct);

        if (ratings.Count < MinRatingsToTrain)
        {
            _logger.LogWarning(
                "Retrain job {JobId} skipped: {Count} ratings (need {Min})",
                jobId, ratings.Count, MinRatingsToTrain);
            throw new InvalidOperationException(
                $"Need at least {MinRatingsToTrain} ratings to train; currently {ratings.Count}.");
        }

        var result = await trainer.TrainAsync(ratings, ct);

        await metadataRepo.RecordAsync(
            new MlModelMetadata
            {
                Version = result.Version,
                Rmse = result.Rmse,
                R2 = result.R2,
                SampleCount = result.SampleCount,
                TrainedAt = result.TrainedAt,
            },
            promoteAsActive: true,
            ct);

        await db.SaveChangesAsync(ct);

        var predictor = sp.GetRequiredService<CollaborativeFilteringPredictor>();
        await MaterializePredictionsAsync(db, predictor, result.Version, jobId, ct);

        _logger.LogInformation(
            "Retrain job {JobId} completed: {Version} (RMSE {Rmse:F4}, R² {R2:F4}, {Count} samples)",
            jobId, result.Version, result.Rmse, result.R2, result.SampleCount);

        return result;
    }

    private async Task MaterializePredictionsAsync(
        ApplicationDbContext db,
        CollaborativeFilteringPredictor predictor,
        string version,
        string jobId,
        CancellationToken ct)
    {
        var activeUserIds = await db.Ratings
            .AsNoTracking()
            .Select(r => r.UserId)
            .Distinct()
            .ToListAsync(ct);

        var candidateMovieIds = await db.Ratings
            .AsNoTracking()
            .Select(r => r.MovieId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation(
            "Retrain job {JobId} materializing predictions: {Users} users x up to {Movies} candidates",
            jobId, activeUserIds.Count, candidateMovieIds.Count);

        var deletedSameVersion = await db.MlPredictions
            .Where(p => p.ModelVersion == version)
            .ExecuteDeleteAsync(ct);

        if (deletedSameVersion > 0)
        {
            _logger.LogInformation(
                "Retrain job {JobId} cleared {Count} stale rows from same version", jobId, deletedSameVersion);
        }

        var buffer = new List<MlPrediction>(InsertBatchSize);
        var totalInserted = 0;

        foreach (var userId in activeUserIds)
        {
            ct.ThrowIfCancellationRequested();

            var ratedMovies = await db.Ratings
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(r => r.MovieId)
                .ToListAsync(ct);

            var ratedSet = ratedMovies.ToHashSet();
            var candidates = candidateMovieIds.Where(id => !ratedSet.Contains(id)).ToList();
            if (candidates.Count == 0) continue;

            var predictions = predictor.PredictBatch(userId, candidates);

            var topK = predictions
                .OrderByDescending(kv => kv.Value)
                .Take(MaxPredictionsPerUser);

            foreach (var (movieId, score) in topK)
            {
                buffer.Add(new MlPrediction
                {
                    UserId = userId,
                    MovieId = movieId,
                    PredictedScore = (decimal)score,
                    ModelVersion = version,
                });

                if (buffer.Count >= InsertBatchSize)
                {
                    totalInserted += await FlushAsync(db, buffer, ct);
                }
            }
        }

        if (buffer.Count > 0)
        {
            totalInserted += await FlushAsync(db, buffer, ct);
        }

        var evicted = await db.MlPredictions
            .Where(p => p.ModelVersion != version)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Retrain job {JobId} stored {Inserted} predictions; evicted {Evicted} from older versions",
            jobId, totalInserted, evicted);
    }

    private static async Task<int> FlushAsync(
        ApplicationDbContext db,
        List<MlPrediction> buffer,
        CancellationToken ct)
    {
        db.MlPredictions.AddRange(buffer);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var count = buffer.Count;
        buffer.Clear();
        return count;
    }

    private static string NewJobId() => $"retrain-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
}
