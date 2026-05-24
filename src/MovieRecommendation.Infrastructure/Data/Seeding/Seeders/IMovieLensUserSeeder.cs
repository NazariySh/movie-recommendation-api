namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface IMovieLensUserSeeder
{
    Task<IReadOnlyDictionary<int, Guid>> SeedAsync(int count, CancellationToken cancellationToken = default);
}
