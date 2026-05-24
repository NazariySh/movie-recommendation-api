using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

public class SemanticSearchMoviesQueryHandler
    : IQueryHandler<SemanticSearchMoviesQuery, SemanticSearchMoviesResult>
{
    private readonly ISearchEngine _searchEngine;
    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public SemanticSearchMoviesQueryHandler(
        ISearchEngine searchEngine,
        IMovieRepository movieRepository,
        ICacheService cache)
    {
        _searchEngine = searchEngine;
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public Task<SemanticSearchMoviesResult> Handle(
        SemanticSearchMoviesQuery query,
        CancellationToken cancellationToken)
    {
        var trimmed = query.Query?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Task.FromResult(new SemanticSearchMoviesResult([], query.Query ?? string.Empty, 0));
        }

        var key = SearchCacheKeys.Semantic(query.Lang, trimmed, query.Limit, query.MinScore);
        return _cache.GetOrSetAsync(key, ct => ComputeAsync(trimmed, ct), SearchCacheTtls.Semantic, cancellationToken);

        async Task<SemanticSearchMoviesResult> ComputeAsync(string q, CancellationToken ct)
        {
            var scored = await _searchEngine.SemanticSearchAsync(q, query.Limit, ct);

            var qualifyingIds = scored
                .Where(s => s.Score >= query.MinScore)
                .Select(s => s.MovieId)
                .ToList();

            if (qualifyingIds.Count == 0)
            {
                return new SemanticSearchMoviesResult([], q, 0);
            }

            var movies = await _movieRepository.GetListItemsByIdsAsync(qualifyingIds, query.Lang, ct);

            var orderById = qualifyingIds
                .Select((id, idx) => (id, idx))
                .ToDictionary(x => x.id, x => x.idx);

            var items = movies
                .OrderBy(m => orderById[m.Id])
                .ToList();

            return new SemanticSearchMoviesResult(items, q, items.Count);
        }
    }
}
