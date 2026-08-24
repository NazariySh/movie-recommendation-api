using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Domain.Entities.Reviews;

public class ReviewHelpfulVote
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ReviewId { get; set; }

    public MovieReview Review { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
