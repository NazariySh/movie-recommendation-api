namespace MovieRecommendation.Application.DTOs.Ratings;

public class UserRatingDto
{
    public Guid MovieId { get; set; }

    public string MovieKey { get; set; } = null!;

    public string MovieTitle { get; set; } = null!;

    public string? PosterUrl { get; set; }

    public decimal Score { get; set; }

    public DateTime UpdatedAt { get; set; }
}
