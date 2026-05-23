namespace MovieRecommendation.Application.DTOs.Reviews;

public class UpdateMovieReviewDto
{
    public string Body { get; set; } = null!;

    public bool IsSpoiler { get; set; }

    public decimal? Score { get; set; }
}
