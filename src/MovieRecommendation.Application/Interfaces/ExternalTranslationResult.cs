namespace MovieRecommendation.Application.Interfaces;

public class ExternalTranslationResult
{
    public string LanguageCode { get; set; } = null!;

    public string? Title { get; set; }

    public string? Overview { get; set; }

    public string? Tagline { get; set; }
}
