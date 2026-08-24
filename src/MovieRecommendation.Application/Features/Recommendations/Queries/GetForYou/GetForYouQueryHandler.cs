using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetForYou;

public class GetForYouQueryHandler : IQueryHandler<GetForYouQuery, IReadOnlyList<MovieDto>>
{
    private readonly IRecommendationEngine _engine;
    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetForYouQueryHandler(
        IRecommendationEngine engine,
        IMovieRepository movieRepository,
        ICacheService cache)
    {
        _engine = engine;
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public Task<IReadOnlyList<MovieDto>> Handle(GetForYouQuery request, CancellationToken cancellationToken)
    {
        var key = RecommendationCacheKeys.ForYou(request.UserId, request.Count, request.Lang);
        return _cache.GetOrSetAsync(key, ComputeAsync, RecommendationCacheTtls.ForYou, cancellationToken);

        async Task<IReadOnlyList<MovieDto>> ComputeAsync(CancellationToken ct)
        {
            var scored = await _engine.GetPersonalRecommendationsAsync(request.UserId, request.Count, ct);

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
