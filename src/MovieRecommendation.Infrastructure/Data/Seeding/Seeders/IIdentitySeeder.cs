namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
