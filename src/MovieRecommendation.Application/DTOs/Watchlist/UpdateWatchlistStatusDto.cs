using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Watchlist;

public class UpdateWatchlistStatusDto
{
    public WatchlistStatus Status { get; set; }

    public string? Notes { get; set; }
}
