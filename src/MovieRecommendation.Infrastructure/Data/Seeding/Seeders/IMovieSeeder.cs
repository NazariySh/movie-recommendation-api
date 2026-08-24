using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public interface IMovieSeeder
{
    Task SeedAsync(IReadOnlyList<MovieLensRecord> source, int maxMovies, CancellationToken cancellationToken = default);
}
