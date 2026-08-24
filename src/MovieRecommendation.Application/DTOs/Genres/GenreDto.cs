namespace MovieRecommendation.Application.DTOs.Genres;

public class GenreDto
{
    public int Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;
}
