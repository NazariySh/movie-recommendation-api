using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetTrendingMovies;

public class GetTrendingMoviesQueryHandler
    : IQueryHandler<GetTrendingMoviesQuery, IReadOnlyList<MovieDto>>
{
    private const double HalfLifeDays = 3.0;
    private const double RatingCentre = 2.5;
    private const int MinRatingsInWindow = 3;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetTrendingMoviesQueryHandler(
        IMovieRepository movieRepository,
        ICacheService cache)
    {
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public Task<IReadOnlyList<MovieDto>> Handle(GetTrendingMoviesQuery request, CancellationToken cancellationToken)
    {
        var key = RecommendationCacheKeys.Trending(request.Type, request.DaysWindow, request.Count, request.Lang);
        return _cache.GetOrSetAsync(key, ComputeAsync, CacheTtl, cancellationToken);

        async Task<IReadOnlyList<MovieDto>> ComputeAsync(CancellationToken ct)
        {
            var ids = await _movieRepository.GetTrendingIdsAsync(
                type: request.Type,
                daysWindow: request.DaysWindow,
                halfLifeDays: HalfLifeDays,
                ratingCentre: RatingCentre,
                minRatings: MinRatingsInWindow,
                limit: request.Count,
                ct);

            if (ids.Count == 0)
            {
                return [];
            }

            var movies = await _movieRepository.GetByIdsAsync(ids, request.Lang, ct);

            var order = ids
                .Select((id, index) => (id, index))
                .ToDictionary(x => x.id, x => x.index);

            return movies
                .OrderBy(m => order[m.Id])
                .ToList();
        }
    }
}
