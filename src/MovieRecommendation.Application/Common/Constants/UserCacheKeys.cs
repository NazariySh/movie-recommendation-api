namespace MovieRecommendation.Application.Common.Constants;

public static class UserCacheKeys
{
    public const string StatsPrefix = "users:stats:";

    public static string Stats(Guid userId, string lang) => $"users:stats:{userId}:{lang}";
}
