namespace MovieRecommendation.Application.DTOs.Admin;

public class AdminUserActivityDto
{
    public int TotalRatings { get; set; }

    public int TotalReviews { get; set; }

    public int TotalReplies { get; set; }

    public int WatchlistCount { get; set; }

    public DateTime? LastActivityAt { get; set; }
}
