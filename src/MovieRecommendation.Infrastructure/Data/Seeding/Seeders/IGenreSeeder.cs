namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface IGenreSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
