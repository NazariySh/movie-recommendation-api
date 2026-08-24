namespace MovieRecommendation.Application.Common.Constants;

public static class GenreCacheKeys
{
    public const string Prefix = "genres:";

    public static string All(string lang) => $"genres:all:{lang}";
}
