using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetBecauseYouLiked;

public class GetBecauseYouLikedQueryHandler : IQueryHandler<GetBecauseYouLikedQuery, IReadOnlyList<MovieDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly IRecommendationEngine _engine;
    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetBecauseYouLikedQueryHandler(
        IRecommendationEngine engine,
        IMovieRepository movieRepository,
        ICacheService cache)
    {
        _engine = engine;
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public Task<IReadOnlyList<MovieDto>> Handle(GetBecauseYouLikedQuery request, CancellationToken cancellationToken)
    {
        var key = RecommendationCacheKeys.Because(request.MovieId, request.CurrentUserId, request.Count, request.Lang);
        return _cache.GetOrSetAsync(key, ComputeAsync, CacheTtl, cancellationToken);

        async Task<IReadOnlyList<MovieDto>> ComputeAsync(CancellationToken ct)
        {
            var scored = request.CurrentUserId == Guid.Empty
                ? await _engine.GetSimilarMoviesAsync(request.MovieId, request.Count, ct)
                : await _engine.GetBecauseYouWatchedAsync(request.CurrentUserId, request.MovieId, request.Count, ct);

            var ids = scored.Select(s => s.MovieId).ToList();
            if (ids.Count == 0) return [];

            var movies = await _movieRepository.GetByIdsAsync(ids, request.Lang, ct);
            var reasonById = scored.ToDictionary(s => s.MovieId, s => s.Reason);

            foreach (var movie in movies)
            {
                if (reasonById.TryGetValue(movie.Id, out var reason))
                {
                    movie.RecommendationReason = reason;
                }
            }

            return movies
                .OrderBy(m => ids.IndexOf(m.Id))
                .ToList();
        }
    }
}
