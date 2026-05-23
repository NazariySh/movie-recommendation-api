using MovieRecommendation.Application.Common.Models;

namespace MovieRecommendation.Application.Interfaces.ML;

public interface IRecommendationEngine
{
    Task<List<ScoredMovie>> GetPersonalRecommendationsAsync(Guid userId, int limit = 20, CancellationToken ct = default);

    Task<List<ScoredMovie>> GetSimilarMoviesAsync(Guid movieId, int limit = 10, CancellationToken ct = default);

    Task<List<ScoredMovie>> GetBecauseYouWatchedAsync(Guid userId, Guid sourceMovieId, int limit = 10, CancellationToken ct = default);

    Task<List<ScoredMovie>> GetColdStartRecommendationsAsync(IEnumerable<int> genreIds, int limit = 20, CancellationToken ct = default);
}
