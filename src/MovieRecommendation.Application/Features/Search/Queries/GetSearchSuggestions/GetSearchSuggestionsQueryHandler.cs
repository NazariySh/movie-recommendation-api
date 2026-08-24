using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Search.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsQueryHandler
    : IQueryHandler<GetSearchSuggestionsQuery, SearchSuggestionsDto>
{
    private const int MinQueryLength = 2;
    private const int MinSemanticFallbackLength = 4;
    private const double SemanticFallbackMinScore = 0.50;

    private readonly IMovieRepository _movieRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ISearchEngine _searchEngine;
    private readonly ICacheService _cache;

    public GetSearchSuggestionsQueryHandler(
        IMovieRepository movieRepository,
        IArtistRepository artistRepository,
        ISearchEngine searchEngine,
        ICacheService cache)
    {
        _movieRepository = movieRepository;
        _artistRepository = artistRepository;
        _searchEngine = searchEngine;
        _cache = cache;
    }

    public async Task<SearchSuggestionsDto> Handle(GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var trimmed = request.Query?.Trim() ?? string.Empty;

        if (trimmed.Length < MinQueryLength)
        {
            return new SearchSuggestionsDto();
        }

        var key = SearchCacheKeys.Suggestions(request.Lang, trimmed, request.Limit);

        return await _cache.GetOrSetAsync(key, ct => ComputeAsync(trimmed, ct), SearchCacheTtls.Suggestions, cancellationToken);

        async Task<SearchSuggestionsDto> ComputeAsync(string q, CancellationToken ct)
        {
            var movies = await _movieRepository.SearchSuggestionsAsync(q, type: null, request.Limit, request.Lang, ct);
            var artists = await _artistRepository.SearchSuggestionsAsync(q, request.Limit, ct);

            if (movies.Count == 0 && q.Length >= MinSemanticFallbackLength)
            {
                movies = await SemanticFallbackAsync(q, request.Limit, request.Lang, ct);
            }

            return new SearchSuggestionsDto
            {
                Movies = movies,
                Artists = artists,
            };
        }
    }

    private async Task<IReadOnlyList<MovieSuggestionDto>> SemanticFallbackAsync(
        string query,
        int limit,
        string lang,
        CancellationToken ct)
    {
        var scored = await _searchEngine.SemanticSearchAsync(query, limit, ct);
        var orderedIds = scored
            .Where(s => s.Score >= SemanticFallbackMinScore)
            .Select(s => s.MovieId)
            .ToList();

        if (orderedIds.Count == 0)
        {
            return [];
        }

        var hydrated = await _movieRepository.GetSuggestionsByIdsAsync(orderedIds, lang, ct);
        var orderById = orderedIds
            .Select((id, idx) => (id, idx))
            .ToDictionary(x => x.id, x => x.idx);

        return hydrated
            .OrderBy(m => orderById[m.Id])
            .ToList();
    }
}
