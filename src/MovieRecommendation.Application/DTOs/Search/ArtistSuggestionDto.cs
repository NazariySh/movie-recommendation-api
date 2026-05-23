namespace MovieRecommendation.Application.DTOs.Search;

public class ArtistSuggestionDto
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public string? KnownFor { get; set; }
}
