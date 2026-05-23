namespace MovieRecommendation.Application.Common.Constants;

public static class MovieCacheKeys
{
    public const string ListPrefix = "movies:list";
    public const string DetailPrefix = "movies:detail:";
    public const string SimilarPrefix = "movies:similar:";

    public static string Detail(Guid movieId, string lang, Guid currentUserId)
        => $"movies:detail:{movieId}:{lang}:{currentUserId}";

    public static string Similar(Guid movieId, int count, string lang)
        => $"movies:similar:{movieId}:{count}:{lang}";

    public static string DetailFor(Guid movieId) => $"movies:detail:{movieId}";

    public static string SimilarFor(Guid movieId) => $"movies:similar:{movieId}";
}
