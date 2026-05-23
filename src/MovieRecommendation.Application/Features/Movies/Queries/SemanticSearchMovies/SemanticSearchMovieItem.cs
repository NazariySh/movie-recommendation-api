namespace MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

public record SemanticSearchMovieItem(
    Guid Id,
    string Title,
    string? Overview,
    string? PosterUrl,
    int? ReleaseYear,
    decimal VoteAverage,
    double SimilarityScore
);
