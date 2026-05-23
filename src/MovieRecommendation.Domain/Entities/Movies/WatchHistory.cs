using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Domain.Entities.Movies;

public class WatchHistory : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public DateTime WatchedAt { get; set; }
}
