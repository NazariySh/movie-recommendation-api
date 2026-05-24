namespace MovieRecommendation.Application.DTOs.Admin;

public class DashboardModelStatusDto
{
    public DateTime? LastTrainedAt { get; set; }

    public int? SamplesCount { get; set; }

    public double? Rmse { get; set; }
}
