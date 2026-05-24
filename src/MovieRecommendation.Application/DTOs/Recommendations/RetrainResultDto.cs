namespace MovieRecommendation.Application.DTOs.Recommendations;

public class RetrainResultDto
{
    public string JobId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime TriggeredAt { get; set; }
}
