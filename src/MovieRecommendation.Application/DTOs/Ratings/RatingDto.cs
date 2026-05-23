namespace MovieRecommendation.Application.DTOs.Ratings;

public class RatingDto
{
    public Guid MovieId { get; set; }

    public decimal Score { get; set; }

    public DateTime UpdatedAt { get; set; }
}
