namespace MovieRecommendation.Application.Interfaces;

public class TmdbPersonResult
{
    public int TmdbId { get; set; }

    public string? ImdbId { get; set; }

    public string Name { get; set; } = null!;

    public string? Biography { get; set; }

    public DateOnly? Birthday { get; set; }

    public DateOnly? DateOfDeath { get; set; }

    public string? PlaceOfBirth { get; set; }

    public string? KnownForDepartment { get; set; }

    public string? Gender { get; set; }

    public string? ProfileImageUrl { get; set; }
}
