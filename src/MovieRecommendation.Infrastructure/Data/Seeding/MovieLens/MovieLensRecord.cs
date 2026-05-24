namespace MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

public sealed record MovieLensRecord(
    int MovieLensId,
    string Title,
    int? Year,
    string? ImdbId,
    int? TmdbId,
    IReadOnlyList<string> Genres);
