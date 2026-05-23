namespace MovieRecommendation.Application.DTOs.Artists;

public class UpdateArtistDto
{
    public string Name { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public DateOnly? Birthday { get; set; }

    public DateOnly? DateOfDeath { get; set; }

    public string? PlaceOfBirth { get; set; }

    public string? Nationality { get; set; }

    public string? Gender { get; set; }

    public string? KnownForDepartment { get; set; }

    public string? Biography { get; set; }
}
