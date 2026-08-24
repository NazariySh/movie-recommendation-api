using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

public record SemanticSearchMoviesResult(
    List<MovieListItemDto> Items,
    string Query,
    int Total
);
