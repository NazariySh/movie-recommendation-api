using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.ML.Models;
using MovieRecommendation.ML.Storage;

namespace MovieRecommendation.ML.Trainers;

public class RecommendationModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly MlModelStorage _storage;
    private readonly RecommendationSettings _settings;
    private readonly ILogger<RecommendationModelTrainer> _logger;

    public RecommendationModelTrainer(
        MLContext mlContext,
        MlModelStorage storage,
        IOptions<RecommendationSettings> settings,
        ILogger<RecommendationModelTrainer> logger)
    {
        _mlContext = mlContext;
        _storage = storage;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TrainingResult> TrainAsync(
        IReadOnlyCollection<MovieRatingData> ratings,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting ML.NET Matrix Factorization training on {Count} ratings...", ratings.Count);

        var dataView = _mlContext.Data.LoadFromEnumerable(ratings);

        var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = _mlContext.Transforms
            .Conversion.MapValueToKey(
                outputColumnName: "UserIdEncoded",
                inputColumnName: nameof(MovieRatingData.UserId))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(
                outputColumnName: "MovieIdEncoded",
                inputColumnName: nameof(MovieRatingData.MovieId)))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                new MatrixFactorizationTrainer.Options
                {
                    MatrixColumnIndexColumnName = "UserIdEncoded",
                    MatrixRowIndexColumnName = "MovieIdEncoded",
                    LabelColumnName = "Label",
                    NumberOfIterations = _settings.MatrixFactorizationIterations,
                    ApproximationRank = _settings.MatrixFactorizationRank,
                    LearningRate = _settings.MatrixFactorizationLearningRate,
                    Lambda = _settings.MatrixFactorizationLambda,
                    Quiet = false,
                }));

        var model = await Task.Run(() => pipeline.Fit(split.TrainSet), ct);

        var predictions = model.Transform(split.TestSet);
        var metrics = _mlContext.Regression.Evaluate(predictions);

        _logger.LogInformation(
            "Model trained. RMSE: {Rmse:F4} | R²: {R2:F4}",
            metrics.RootMeanSquaredError,
            metrics.RSquared);

        var trainedAt = DateTime.UtcNow;
        string version = $"v{trainedAt:yyyyMMdd_HHmmss}";
        _storage.SaveModel(model, dataView.Schema, version);

        return new TrainingResult(
            Version: version,
            Rmse: metrics.RootMeanSquaredError,
            R2: metrics.RSquared,
            SampleCount: ratings.Count,
            TrainedAt: trainedAt);
    }
}
