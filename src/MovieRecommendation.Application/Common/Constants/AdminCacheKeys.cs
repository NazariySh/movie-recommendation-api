namespace MovieRecommendation.Application.Common.Constants;

public static class AdminCacheKeys
{
    public const string DashboardSummary = "admin:dashboard:summary";
    public const string DashboardActivityPrefix = "admin:dashboard:activity:";

    public static string DashboardActivity(int days) => $"{DashboardActivityPrefix}{days}";
}
