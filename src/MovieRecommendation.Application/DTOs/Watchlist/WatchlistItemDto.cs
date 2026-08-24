using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Watchlist;

public class WatchlistItemDto
{
    public Guid MovieId { get; set; }

    public string MovieKey { get; set; } = null!;

    public string MovieTitle { get; set; } = null!;

    public string? PosterUrl { get; set; }

    public WatchlistStatus Status { get; set; }

    public DateOnly? WatchedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime UpdatedAt { get; set; }
}
