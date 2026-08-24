using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;
using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;
using MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

namespace MovieRecommendation.Infrastructure.Data.Seeding;

public sealed class DataSeeder
{
    private readonly SeedingSettings _settings;
    private readonly IIdentitySeeder _identitySeeder;
    private readonly IGenreSeeder _genreSeeder;
    private readonly IMovieSeeder _movieSeeder;
    private readonly IMovieLensUserSeeder _userSeeder;
    private readonly IMovieLensRatingSeeder _ratingSeeder;
    private readonly IMovieLensCsvReader _csvReader;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        IOptions<SeedingSettings> settings,
        IIdentitySeeder identitySeeder,
        IGenreSeeder genreSeeder,
        IMovieSeeder movieSeeder,
        IMovieLensUserSeeder userSeeder,
        IMovieLensRatingSeeder ratingSeeder,
        IMovieLensCsvReader csvReader,
        ILogger<DataSeeder> logger)
    {
        _settings = settings.Value;
        _identitySeeder = identitySeeder;
        _genreSeeder = genreSeeder;
        _movieSeeder = movieSeeder;
        _userSeeder = userSeeder;
        _ratingSeeder = ratingSeeder;
        _csvReader = csvReader;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _identitySeeder.SeedAsync(cancellationToken);
        await _genreSeeder.SeedAsync(cancellationToken);

        var paths = ResolveDataPaths();
        if (paths is null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var records = _csvReader.Read(paths.MoviesPath, paths.LinksPath);
        _logger.LogInformation("Loaded {Count} MovieLens records from {Path}", records.Count, paths.MoviesPath);

        await _movieSeeder.SeedAsync(records, _settings.MovieCount, cancellationToken);

        var userMap = await _userSeeder.SeedAsync(_settings.MovieLensUserCount, cancellationToken);

        await _ratingSeeder.SeedAsync(paths.RatingsPath, records, userMap, _settings.MaxRatingsPerUser, cancellationToken);
    }

    private DataFiles? ResolveDataPaths()
    {
        var dataDir = _settings.DataPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", SeedingConstants.DefaultDataDirectory);

        var moviesPath = Path.Combine(dataDir, SeedingConstants.MoviesFileName);
        var linksPath = Path.Combine(dataDir, SeedingConstants.LinksFileName);
        var ratingsPath = Path.Combine(dataDir, SeedingConstants.RatingsFileName);

        if (!File.Exists(moviesPath))
        {
            _logger.LogError("MovieLens dataset missing: {Path}", moviesPath);
            return null;
        }

        return new DataFiles(moviesPath, linksPath, ratingsPath);
    }

    private sealed record DataFiles(string MoviesPath, string LinksPath, string RatingsPath);
}
