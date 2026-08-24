using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Domain.Entities.Predictions;

public class MlPrediction : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public decimal PredictedScore { get; set; }

    public string ModelVersion { get; set; }
}
