using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Domain.Entities.Movies;

public class WatchlistItem : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public WatchlistStatus Status { get; set; }

    public DateOnly? WatchedAt { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
}
