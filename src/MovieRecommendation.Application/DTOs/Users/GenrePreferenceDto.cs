namespace MovieRecommendation.Application.DTOs.Users;

public class GenrePreferenceDto
{
    public int GenreId { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Weight { get; set; }
}
