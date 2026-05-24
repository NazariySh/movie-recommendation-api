using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.DTOs.Recommendations;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Predictions;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.ML.Models;
using MovieRecommendation.ML.Predictors;

namespace MovieRecommendation.ML.Trainers;

public class ModelRetrainingOrchestrator : IModelRetrainingOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly RecommendationSettings _settings;
    private readonly ILogger<ModelRetrainingOrchestrator> _logger;

    public ModelRetrainingOrchestrator(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        IOptions<RecommendationSettings> settings,
        ILogger<ModelRetrainingOrchestrator> logger)
    {
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RetrainResultDto> RunAsync(Guid? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var jobId = NewJobId();
        var triggeredAt = DateTime.UtcNow;

        var result = await ExecuteAsync(jobId, actorUserId, cancellationToken);

        return new RetrainResultDto
        {
            JobId = jobId,
            Status = $"Completed: {result.Version} (RMSE {result.Rmse:F4})",
            TriggeredAt = triggeredAt,
        };
    }

    public RetrainResultDto Enqueue(Guid? actorUserId = null)
    {
        var jobId = NewJobId();
        var triggeredAt = DateTime.UtcNow;

        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteAsync(jobId, actorUserId, _lifetime.ApplicationStopping);
            }
            catch (OperationCanceledException) when (_lifetime.ApplicationStopping.IsCancellationRequested)
            {
                _logger.LogInformation("Background retrain job {JobId} cancelled by app shutdown", jobId);
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

    private async Task<TrainingResult> ExecuteAsync(string jobId, Guid? actorUserId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var trainer = sp.GetRequiredService<RecommendationModelTrainer>();
        var metadataRepo = sp.GetRequiredService<IMlModelMetadataRepository>();

        _logger.LogInformation(
            "Retrain job {JobId} fetching ratings (triggered by {ActorUserId})...",
            jobId,
            actorUserId?.ToString() ?? "system");

        var ratings = await db.Ratings
            .AsNoTracking()
            .Select(r => new MovieRatingData
            {
                UserId = r.UserId.ToString(),
                MovieId = r.MovieId.ToString(),
                Label = (float)r.Score,
            })
            .ToListAsync(ct);

        if (ratings.Count < _settings.MinRatingsToTrain)
        {
            _logger.LogWarning(
                "Retrain job {JobId} skipped: {Count} ratings (need {Min})",
                jobId, ratings.Count, _settings.MinRatingsToTrain);
            throw new InvalidOperationException(
                $"Need at least {_settings.MinRatingsToTrain} ratings to train; currently {ratings.Count}.");
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
        var ratingsByUser = await db.Ratings
            .AsNoTracking()
            .GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, MovieIds = g.Select(r => r.MovieId).ToList() })
            .ToDictionaryAsync(x => x.UserId, x => x.MovieIds, ct);

        var candidateMovieIds = ratingsByUser
            .SelectMany(kv => kv.Value)
            .Distinct()
            .ToList();

        _logger.LogInformation(
            "Retrain job {JobId} materializing predictions: {Users} users x up to {Movies} candidates",
            jobId, ratingsByUser.Count, candidateMovieIds.Count);

        var deletedSameVersion = await db.MlPredictions
            .Where(p => p.ModelVersion == version)
            .ExecuteDeleteAsync(ct);

        if (deletedSameVersion > 0)
        {
            _logger.LogInformation(
                "Retrain job {JobId} cleared {Count} stale rows from same version", jobId, deletedSameVersion);
        }

        var buffer = new List<MlPrediction>(_settings.PredictionInsertBatchSize);
        var totalInserted = 0;

        foreach (var (userId, ratedMovies) in ratingsByUser)
        {
            ct.ThrowIfCancellationRequested();

            var ratedSet = ratedMovies.ToHashSet();
            var candidates = candidateMovieIds.Where(id => !ratedSet.Contains(id)).ToList();
            if (candidates.Count == 0) continue;

            var predictions = predictor.PredictBatch(userId, candidates);

            var topK = predictions
                .OrderByDescending(kv => kv.Value)
                .Take(_settings.MaxPredictionsPerUser);

            foreach (var (movieId, score) in topK)
            {
                buffer.Add(new MlPrediction
                {
                    UserId = userId,
                    MovieId = movieId,
                    PredictedScore = (decimal)score,
                    ModelVersion = version,
                });

                if (buffer.Count >= _settings.PredictionInsertBatchSize)
                {
                    totalInserted += await FlushAsync(db, buffer, ct);
                }
            }
        }

        if (buffer.Count > 0)
        {
            totalInserted += await FlushAsync(db, buffer, ct);
        }

        var retainedVersions = await db.MlModelMetadata
            .AsNoTracking()
            .OrderByDescending(m => m.TrainedAt)
            .Select(m => m.Version)
            .Take(Math.Max(1, _settings.RetainPredictionVersions))
            .ToListAsync(ct);

        var evicted = await db.MlPredictions
            .Where(p => !retainedVersions.Contains(p.ModelVersion))
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Retrain job {JobId} stored {Inserted} predictions; retained versions {Versions}; evicted {Evicted} from older",
            jobId, totalInserted, string.Join(",", retainedVersions), evicted);
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

    private static string NewJobId()
    {
        var raw = $"retrain-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        return raw[..Math.Min(32, raw.Length)];
    }
}
