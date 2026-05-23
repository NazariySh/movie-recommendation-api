namespace MovieRecommendation.Application.DTOs.Movies;

public class MovieTranslationDto
{
    public string LanguageCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Overview { get; set; }

    public string? Tagline { get; set; }
}
