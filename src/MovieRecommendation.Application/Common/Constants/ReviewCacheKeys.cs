namespace MovieRecommendation.Application.Common.Constants;

public static class ReviewCacheKeys
{
    public const string MoviePrefix = "reviews:movie";

    public static string ForMovie(Guid movieId) => $"reviews:movie:{movieId}";

    public static string MovieReviewsPage(Guid movieId, Guid viewerId, string? sort, int pageNumber, int pageSize)
        => string.Join(':',
            MoviePrefix,
            movieId,
            viewerId,
            sort ?? string.Empty,
            pageNumber,
            pageSize);
}
