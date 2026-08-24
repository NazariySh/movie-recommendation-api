using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetRecommendations;

public class GetRecommendationsQueryHandler : IQueryHandler<GetRecommendationsQuery, IReadOnlyList<MovieDto>>
{
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IMovieRepository _movieRepository;

    public GetRecommendationsQueryHandler(IRecommendationEngine recommendationEngine, IMovieRepository movieRepository)
    {
        _recommendationEngine = recommendationEngine;
        _movieRepository = movieRepository;
    }

    public async Task<IReadOnlyList<MovieDto>> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var scoredMovies = await _recommendationEngine.GetPersonalRecommendationsAsync(request.UserId, request.Count, cancellationToken);

        var movieIds = scoredMovies.Select(m => m.MovieId).ToList();
        if (movieIds.Count == 0)
        {
            return [];
        }

        var movies = await _movieRepository.GetByIdsAsync(movieIds, request.Lang, cancellationToken);

        return movies
            .OrderBy(m => movieIds.IndexOf(m.Id))
            .ToList();
    }
}
