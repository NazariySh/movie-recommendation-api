namespace MovieRecommendation.Domain.Entities.Movies;

public class GenreTranslation
{
    public int GenreId { get; set; }

    public Genre Genre { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public string Name { get; set; } = null!;
}
