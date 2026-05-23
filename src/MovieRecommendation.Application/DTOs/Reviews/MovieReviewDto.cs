namespace MovieRecommendation.Application.DTOs.Reviews;

public class MovieReviewDto
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }

    public Guid UserId { get; set; }

    public Guid? ParentReviewId { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? AuthorAvatarUrl { get; set; }

    public string Body { get; set; } = null!;

    public bool IsSpoiler { get; set; }

    public decimal? Score { get; set; }

    public int HelpfulCount { get; set; }

    public bool MarkedHelpful { get; set; }

    public int ReplyCount { get; set; }

    public string? ReplyToUserName { get; set; }

    public DateTime CreatedAt { get; set; }
}
