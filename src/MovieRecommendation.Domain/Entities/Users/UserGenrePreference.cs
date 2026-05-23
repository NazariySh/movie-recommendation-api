using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Domain.Entities.Users;

public class UserGenrePreference : BaseEntity, ISoftDeletable
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int GenreId { get; set; }

    public Genre Genre { get; set; } = null!;

    public decimal Weight { get; set; }

    public bool IsDeleted { get; set; }
}
