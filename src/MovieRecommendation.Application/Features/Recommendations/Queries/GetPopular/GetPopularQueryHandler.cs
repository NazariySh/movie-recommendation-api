using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetPopular;

public class GetPopularQueryHandler : IQueryHandler<GetPopularQuery, IReadOnlyList<MovieDto>>
{
    private const int MinRatingsForLeaderboard = 10;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetPopularQueryHandler(IMovieRepository movieRepository, ICacheService cache)
    {
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public Task<IReadOnlyList<MovieDto>> Handle(GetPopularQuery request, CancellationToken cancellationToken)
    {
        var key = RecommendationCacheKeys.Popular(request.Type, request.GenreSlug, request.Count, request.Lang);

        return _cache.GetOrSetAsync(key, ComputeAsync, CacheTtl, cancellationToken);

        async Task<IReadOnlyList<MovieDto>> ComputeAsync(CancellationToken ct)
        {
            var ids = await _movieRepository.GetPopularIdsAsync(
                type: request.Type,
                genreSlug: request.GenreSlug,
                minRatings: MinRatingsForLeaderboard,
                limit: request.Count,
                ct);

            if (ids.Count == 0) return [];

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
