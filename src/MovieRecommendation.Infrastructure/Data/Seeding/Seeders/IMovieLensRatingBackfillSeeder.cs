namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface IMovieLensRatingBackfillSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
