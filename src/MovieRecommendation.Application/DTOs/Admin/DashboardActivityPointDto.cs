namespace MovieRecommendation.Application.DTOs.Admin;

public class DashboardActivityPointDto
{
    public DateOnly Date { get; set; }

    public int NewUsers { get; set; }

    public int Ratings { get; set; }

    public int Reviews { get; set; }
}
