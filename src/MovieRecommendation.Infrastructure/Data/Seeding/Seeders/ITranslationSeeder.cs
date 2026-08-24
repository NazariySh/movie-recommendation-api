namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface ITranslationSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
