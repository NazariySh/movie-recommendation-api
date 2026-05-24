namespace MovieRecommendation.Application.DTOs.WatchHistory;

public class WatchHistoryDto
{
    public Guid MovieId { get; set; }

    public string MovieKey { get; set; } = null!;

    public string MovieTitle { get; set; } = null!;

    public string? PosterUrl { get; set; }

    public DateTime WatchedAt { get; set; }
}
