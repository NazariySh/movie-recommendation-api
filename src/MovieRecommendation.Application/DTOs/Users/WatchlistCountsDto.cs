namespace MovieRecommendation.Application.DTOs.Users;

public class WatchlistCountsDto
{
    public int PlanToWatch { get; set; }

    public int Watching { get; set; }

    public int Completed { get; set; }

    public int Dropped { get; set; }
}
