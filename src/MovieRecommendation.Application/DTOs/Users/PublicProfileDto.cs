namespace MovieRecommendation.Application.DTOs.Users;

public class PublicProfileDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; }

    public int TotalRatings { get; set; }

    public int TotalReviews { get; set; }

    public int CompletedCount { get; set; }

    public decimal? AverageRatingGiven { get; set; }
}
