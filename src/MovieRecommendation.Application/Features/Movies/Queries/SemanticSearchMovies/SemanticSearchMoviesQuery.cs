using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

public record SemanticSearchMoviesQuery(
    string Query,
    string Lang = "en",
    int Limit = 20,
    double MinScore = 0.70
) : IQuery<SemanticSearchMoviesResult>;
