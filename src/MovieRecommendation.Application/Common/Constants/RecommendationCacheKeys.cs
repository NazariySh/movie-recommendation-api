using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Common.Constants;

public static class RecommendationCacheKeys
{
    public const string ForYouPrefix = "recommendations:for-you:";
    public const string ColdStartPrefix = "recommendations:cold-start:";
    public const string PopularPrefix = "recommendations:popular:";
    public const string BecausePrefix = "recommendations:because:";
    public const string TrendingPrefix = "recommendations:trending:";

    public static string ForYou(Guid userId, int count, string lang)
        => $"recommendations:for-you:{userId}:{count}:{lang}";

    public static string ColdStart(Guid userId, int count, string lang)
        => $"recommendations:cold-start:{userId}:{count}:{lang}";

    public static string Popular(TitleType? type, string? genreSlug, int count, string lang)
    {
        var typePart = type?.ToString() ?? "any";
        var genrePart = string.IsNullOrWhiteSpace(genreSlug) ? "all" : genreSlug.Trim().ToLower();
        return $"recommendations:popular:{typePart}:{genrePart}:{count}:{lang}";
    }

    public static string Because(Guid movieId, Guid currentUserId, int count, string lang)
    {
        var userPart = currentUserId == Guid.Empty ? "anon" : currentUserId.ToString();
        return $"recommendations:because:{userPart}:{movieId}:{count}:{lang}";
    }

    public static string Trending(TitleType? type, int daysWindow, int count, string lang)
        => $"recommendations:trending:{type}:{daysWindow}:{count}:{lang}";

    public static string ForYouFor(Guid userId) => $"recommendations:for-you:{userId}";

    public static string ColdStartFor(Guid userId) => $"recommendations:cold-start:{userId}";

    public static string BecauseForUser(Guid userId) => $"recommendations:because:{userId}";
}
