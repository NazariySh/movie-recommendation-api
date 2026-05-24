using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Search.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsQueryHandler
    : IQueryHandler<GetSearchSuggestionsQuery, SearchSuggestionsDto>
{
    private const int MinQueryLength = 2;

    private readonly IMovieRepository _movieRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ICacheService _cache;

    public GetSearchSuggestionsQueryHandler(
        IMovieRepository movieRepository,
        IArtistRepository artistRepository,
        ICacheService cache)
    {
        _movieRepository = movieRepository;
        _artistRepository = artistRepository;
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

            return new SearchSuggestionsDto
            {
                Movies = movies,
                Artists = artists,
            };
        }
    }
}
