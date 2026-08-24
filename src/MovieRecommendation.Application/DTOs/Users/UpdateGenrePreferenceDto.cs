namespace MovieRecommendation.Application.DTOs.Users;

public class UpdateGenrePreferenceDto
{
    public int GenreId { get; set; }

    public decimal Weight { get; set; }
}
