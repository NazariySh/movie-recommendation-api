namespace MovieRecommendation.Application.Common.Models;

public record ScoredMovie(
    Guid MovieId,
    double Score,
    string? Reason = null
);
