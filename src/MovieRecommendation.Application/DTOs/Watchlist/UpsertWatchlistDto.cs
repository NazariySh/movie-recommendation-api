using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Watchlist;

public class UpsertWatchlistDto
{
    public Guid MovieId { get; set; }

    public WatchlistStatus Status { get; set; }

    public string? Notes { get; set; }
}
