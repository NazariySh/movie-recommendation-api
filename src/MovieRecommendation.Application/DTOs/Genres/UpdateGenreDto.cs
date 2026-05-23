namespace MovieRecommendation.Application.DTOs.Genres;

public class UpdateGenreDto
{
    public string Slug { get; set; } = null!;

    public List<GenreTranslationDto> Translations { get; set; } = [];
}
