namespace MovieRecommendation.Application.Interfaces;

public class ExternalMovieCast
{
    public string Name { get; set; } = null!;

    public string? ImdbId { get; set; }

    public int? TmdbId { get; set; }

    public string? PhotoUrl { get; set; }

    public string Role { get; set; } = "Acting";

    public string? Character { get; set; }

    public int? CastOrder { get; set; }
}
