using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Domain.Entities.Reviews;

public class MovieReview : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public Guid? ParentReviewId { get; set; }

    public MovieReview? ParentReview { get; set; }

    public string Body { get; set; } = null!;

    public bool IsSpoiler { get; set; }

    public decimal? Score { get; set; }

    public int HelpfulCount { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<MovieReview> Replies { get; set; } = [];

    public ICollection<ReviewHelpfulVote> HelpfulVotes { get; set; } = [];
}
