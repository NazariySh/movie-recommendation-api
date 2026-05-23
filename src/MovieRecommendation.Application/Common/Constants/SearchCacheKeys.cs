namespace MovieRecommendation.Application.Common.Constants;

public static class SearchCacheKeys
{
    public const string SuggestionsPrefix = "search:suggestions:";
    public const string CountsPrefix = "search:counts:";

    public static string Suggestions(string lang, string query, int limit)
        => $"search:suggestions:{lang}:{query.ToLowerInvariant()}:{limit}";

    public static string Counts(string lang, string query)
        => $"search:counts:{lang}:{query.ToLowerInvariant()}";
}
