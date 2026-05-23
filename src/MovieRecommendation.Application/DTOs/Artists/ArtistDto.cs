namespace MovieRecommendation.Application.DTOs.Artists;

public class ArtistDto
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public string? KnownForDepartment { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public int MovieCount { get; set; }
}
