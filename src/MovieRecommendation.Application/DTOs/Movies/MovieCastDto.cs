namespace MovieRecommendation.Application.DTOs.Movies;

public class MovieCastDto
{
    public Guid PersonId { get; set; }

    public string PersonName { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public string Role { get; set; } = null!;

    public string? Character { get; set; }

    public int? CastOrder { get; set; }
}
