using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Search.Queries.GetSearchCounts;

public class GetSearchCountsQueryHandler : IQueryHandler<GetSearchCountsQuery, SearchCountsDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IMovieRepository _movieRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ICacheService _cache;

    public GetSearchCountsQueryHandler(
        IMovieRepository movieRepository,
        IArtistRepository artistRepository,
        ICacheService cache)
    {
        _movieRepository = movieRepository;
        _artistRepository = artistRepository;
        _cache = cache;
    }

    public Task<SearchCountsDto> Handle(GetSearchCountsQuery request, CancellationToken cancellationToken)
    {
        var trimmed = request.Query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Task.FromResult(new SearchCountsDto());
        }

        var key = SearchCacheKeys.Counts(request.Lang, trimmed);
        return _cache.GetOrSetAsync(key, ct => ComputeAsync(trimmed, ct), CacheTtl, cancellationToken);

        async Task<SearchCountsDto> ComputeAsync(string q, CancellationToken ct)
        {
            var movies = await _movieRepository.CountSearchMatchesAsync(q, TitleType.Movie, request.Lang, ct);
            var series = await _movieRepository.CountSearchMatchesAsync(q, TitleType.Series, request.Lang, ct);
            var artists = await _artistRepository.CountSearchMatchesAsync(q, ct);

            return new SearchCountsDto { Movies = movies, Series = series, Artists = artists };
        }
    }
}
