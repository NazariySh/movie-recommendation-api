namespace MovieRecommendation.Application.DTOs.Reviews;

public class CreateReplyDto
{
    public string Body { get; set; } = null!;

    public bool IsSpoiler { get; set; }

    public decimal? Score { get; set; }

    public Guid? ParentReplyId { get; set; }
}
