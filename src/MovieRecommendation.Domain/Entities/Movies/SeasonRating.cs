using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Domain.Entities.Movies;

public class SeasonRating : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid SeasonId { get; set; }

    public Season Season { get; set; } = null!;

    public decimal Score { get; set; }
}
