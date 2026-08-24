namespace MovieRecommendation.Application.Common.Constants;

public static class DashboardWindows
{
    public const int DauDays = 1;
    public const int WeekDays = 7;
    public const int MonthDays = 30;
    public const int TopGenresLimit = 5;
    public const int ActivityDaysMin = 1;
    public const int ActivityDaysMax = 365;
    public const int ActivityDaysDefault = 30;

    public static readonly TimeSpan SummaryCacheTtl = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan ActivityCacheTtl = TimeSpan.FromMinutes(5);
}
