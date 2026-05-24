namespace MovieRecommendation.Application.DTOs.Admin;

public class DashboardSummaryDto
{
    public int TotalUsers { get; set; }

    public int Dau { get; set; }

    public int Mau { get; set; }

    public int TotalMovies { get; set; }

    public int TotalSeries { get; set; }

    public int TotalRatings { get; set; }

    public int RatingsToday { get; set; }

    public int RatingsLast7Days { get; set; }

    public int RatingsLast30Days { get; set; }

    public int TotalReviews { get; set; }

    public IReadOnlyList<DashboardTopGenreDto> TopGenres { get; set; } = [];

    public DashboardModelStatusDto? Model { get; set; }
}
