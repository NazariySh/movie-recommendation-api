namespace MovieRecommendation.Application.DTOs.WatchHistory;

public class CreateWatchHistoryDto
{
    public Guid MovieId { get; set; }

    public DateTime? WatchedAt { get; set; }
}
