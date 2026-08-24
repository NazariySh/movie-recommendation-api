namespace MovieRecommendation.Application.Common.Constants;

public static class SearchCacheKeys
{
    public const string SuggestionsPrefix = "search:suggestions:";
    public const string CountsPrefix = "search:counts:";
    public const string SemanticPrefix = "search:semantic:";

    public static string Suggestions(string lang, string query, int limit)
        => $"search:suggestions:{lang}:{NormalizeQuery(query)}:{limit}";

    public static string Counts(string lang, string query)
        => $"search:counts:{lang}:{NormalizeQuery(query)}";

    public static string Semantic(string lang, string query, int limit, double minScore)
        => $"search:semantic:{lang}:{NormalizeQuery(query)}:{limit}:{minScore:F2}";

    private static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var parts = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }
}
