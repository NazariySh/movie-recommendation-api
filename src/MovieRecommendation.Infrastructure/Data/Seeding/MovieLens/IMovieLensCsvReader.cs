namespace MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

public interface IMovieLensCsvReader
{
    IReadOnlyList<MovieLensRecord> Read(string moviesPath, string linksPath);
}
