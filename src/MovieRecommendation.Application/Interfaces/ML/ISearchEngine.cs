using MovieRecommendation.Application.Common.Models;

namespace MovieRecommendation.Application.Interfaces.ML;

public interface ISearchEngine
{
    Task<List<ScoredMovie>> SemanticSearchAsync(string query, int limit = 20, CancellationToken ct = default);
}
