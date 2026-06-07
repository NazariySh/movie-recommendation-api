using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetAllMovies;

public class GetAllMoviesQueryHandler : IQueryHandler<GetAllMoviesQuery, PagedList<MovieListItemDto>>
{
    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetAllMoviesQueryHandler(IMovieRepository movieRepository, ICacheService cache)
    {
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public async Task<PagedList<MovieListItemDto>> Handle(GetAllMoviesQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKey(request);
        return await _cache.GetOrSetAsync(
            key,
            ct => _movieRepository.GetAllPaginatedAsync(request.Model, request.Lang, ct),
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }

    private static string CacheKey(GetAllMoviesQuery request)
    {
        var m = request.Model;
        return string.Join('|',
            MovieCacheKeys.ListPrefix,
            request.Lang,
            m.PageNumber,
            m.PageSize,
            m.Search ?? string.Empty,
            m.Type?.ToString() ?? string.Empty,
            m.Genres is null ? string.Empty : string.Join(',', m.Genres),
            m.GenreSlugs is null ? string.Empty : string.Join(',', m.GenreSlugs),
            m.ReleaseYears is null ? string.Empty : string.Join(',', m.ReleaseYears),
            m.YearFrom ?? 0,
            m.YearTo ?? 0,
            m.MinRating ?? 0,
            m.MaxRating ?? 0,
            m.MinRatingsCount ?? 0,
            m.RuntimeMin ?? 0,
            m.RuntimeMax ?? 0,
            m.Language ?? string.Empty,
            m.SortBy ?? string.Empty,
            m.SortDescending);
    }
}
