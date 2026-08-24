namespace MovieRecommendation.Application.Common.Constants;

public static class RecommendationCacheTtls
{
    public static readonly TimeSpan ForYou = TimeSpan.FromHours(1);

    public static readonly TimeSpan ColdStart = TimeSpan.FromHours(1);

    public static readonly TimeSpan Popular = TimeSpan.FromHours(6);

    public static readonly TimeSpan Because = TimeSpan.FromHours(6);
}
