using Microsoft.ML;
using MovieRecommendation.ML.Models;
using MovieRecommendation.ML.Storage;

namespace MovieRecommendation.ML.Predictors;

public class CollaborativeFilteringPredictor
{
    private readonly MLContext _mlContext;
    private readonly MlModelStorage _storage;

    private PredictionEngine<MovieRatingData, MovieRatingPrediction>? _engine;
    private string? _loadedVersion;

    public CollaborativeFilteringPredictor(MLContext mlContext, MlModelStorage storage)
    {
        _mlContext = mlContext;
        _storage = storage;
    }

    private const float NeutralScore = 5.5f;

    public float Predict(Guid userId, Guid movieId)
    {
        EnsureEngineLoaded();

        if (_engine is null)
            return 0f;

        var result = _engine.Predict(new MovieRatingData
        {
            UserId = userId.ToString(),
            MovieId = movieId.ToString()
        });

        if (float.IsNaN(result.Score) || float.IsInfinity(result.Score))
            return NeutralScore;

        return Math.Clamp(result.Score, 1.0f, 10.0f);
    }

    public Dictionary<Guid, float> PredictBatch(Guid userId, IEnumerable<Guid> movieIds)
    {
        EnsureEngineLoaded();

        return movieIds.ToDictionary(
            movieId => movieId,
            movieId => Predict(userId, movieId));
    }

    private void EnsureEngineLoaded()
    {
        var latestVersion = _storage.GetLatestVersion();
        if (latestVersion is null)
            return;

        if (_loadedVersion == latestVersion && _engine != null)
            return;

        var model = _storage.LoadModel(latestVersion);
        if (model is null)
            return;

        _engine = _mlContext.Model
            .CreatePredictionEngine<MovieRatingData, MovieRatingPrediction>(model);
        _loadedVersion = latestVersion;
    }
}
