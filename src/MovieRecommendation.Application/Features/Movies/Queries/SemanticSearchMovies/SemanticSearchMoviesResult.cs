namespace MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

public record SemanticSearchMoviesResult(
    List<SemanticSearchMovieItem> Items,
    string Query,
    int Total
);
