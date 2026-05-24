namespace MovieRecommendation.Application.Common.Models;

public record ScoredMovie(
    Guid MovieId,
    double Score,
    RecommendationReason? Reason = null
);

public enum RecommendationReason
{
    ForYou,
    Similar,
    BecauseWatched,
    PopularInGenres,
    TopRated,
    SemanticMatch,
}
