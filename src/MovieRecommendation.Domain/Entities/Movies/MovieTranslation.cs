namespace MovieRecommendation.Domain.Entities.Movies;

public class MovieTranslation
{
    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Overview { get; set; }

    public string? Tagline { get; set; }
}
