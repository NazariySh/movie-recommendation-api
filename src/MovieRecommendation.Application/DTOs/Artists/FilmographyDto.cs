namespace MovieRecommendation.Application.DTOs.Artists;

public class FilmographyDto
{
    public IReadOnlyDictionary<string, IReadOnlyList<FilmographyItemDto>> ByRole { get; set; }
        = new Dictionary<string, IReadOnlyList<FilmographyItemDto>>();
}
