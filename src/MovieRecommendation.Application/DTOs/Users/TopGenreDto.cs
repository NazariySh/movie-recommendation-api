namespace MovieRecommendation.Application.DTOs.Users;

public class TopGenreDto
{
    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Count { get; set; }
}
