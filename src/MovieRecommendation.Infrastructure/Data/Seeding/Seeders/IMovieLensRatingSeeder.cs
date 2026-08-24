using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface IMovieLensRatingSeeder
{
    Task SeedAsync(
        string ratingsPath,
        IReadOnlyList<MovieLensRecord> records,
        IReadOnlyDictionary<int, Guid> movieLensUserIdToDbUserId,
        int maxRatingsPerUser,
        CancellationToken cancellationToken = default);
}
