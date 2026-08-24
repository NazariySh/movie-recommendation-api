namespace MovieRecommendation.Application.DTOs.Users;

public class UserStatsDto
{
    public int TotalRatings { get; set; }

    public int TotalReviews { get; set; }

    public int TotalCommentsReceived { get; set; }

    public WatchlistCountsDto WatchlistCounts { get; set; } = new();

    public decimal? AverageRatingGiven { get; set; }

    public List<int> RatingDistribution { get; set; } = [];

    public List<TopGenreDto> TopGenres { get; set; } = [];

    public int TotalRuntimeMinutesWatched { get; set; }

    public int MemberSinceDays { get; set; }

    public int RatingsThisMonth { get; set; }

    public int LongestStreak { get; set; }
}
