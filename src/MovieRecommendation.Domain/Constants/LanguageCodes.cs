namespace MovieRecommendation.Domain.Constants;

public static class LanguageCodes
{
    public readonly static IReadOnlyList<string> SupportedLanguages = new List<string>()
    {
        { English },
        { Ukrainian }
    };

    public const string Default = English;

    public const string English = "en";
    public const string Ukrainian = "uk";
}
