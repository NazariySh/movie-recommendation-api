namespace MovieRecommendation.Application.DTOs.Admin;

public class DashboardActivityDto
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public IReadOnlyList<DashboardActivityPointDto> Points { get; set; } = [];
}
