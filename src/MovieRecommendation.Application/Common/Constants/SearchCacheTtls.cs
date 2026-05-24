namespace MovieRecommendation.Application.Common.Constants;

public static class SearchCacheTtls
{
    public static readonly TimeSpan Suggestions = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan Counts = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan Semantic = TimeSpan.FromMinutes(10);
}
