using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetSimilarMovies;

public class GetSimilarMoviesQueryHandler
    : IQueryHandler<GetSimilarMoviesQuery, IReadOnlyList<MovieListItemDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IRecommendationEngine _engine;
    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetSimilarMoviesQueryHandler(
        IRecommendationEngine engine,
        IMovieRepository movieRepository,
        ICacheService cache)
    {
        _engine = engine;
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public Task<IReadOnlyList<MovieListItemDto>> Handle(GetSimilarMoviesQuery request, CancellationToken cancellationToken)
    {
        var key = MovieCacheKeys.Similar(request.Id, request.Count, request.Lang);
        return _cache.GetOrSetAsync(key, ComputeAsync, CacheTtl, cancellationToken);

        async Task<IReadOnlyList<MovieListItemDto>> ComputeAsync(CancellationToken ct)
        {
            var scored = await _engine.GetSimilarMoviesAsync(request.Id, request.Count, ct);

            if (scored.Count > 0)
            {
                var ids = scored.Select(s => s.MovieId).ToList();
                var movies = await _movieRepository.GetListItemsByIdsAsync(ids, request.Lang, ct);
                var order = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
                return movies.OrderBy(m => order[m.Id]).ToList();
            }

            return await _movieRepository.GetSimilarAsync(request.Id, request.Count, request.Lang, ct);
        }
    }
}
