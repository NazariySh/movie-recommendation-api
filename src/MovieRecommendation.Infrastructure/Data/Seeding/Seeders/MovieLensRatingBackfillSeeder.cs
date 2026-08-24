using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;
using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class MovieLensRatingBackfillSeeder : IMovieLensRatingBackfillSeeder
{
    private static readonly CsvConfiguration CsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        BadDataFound = null,
        MissingFieldFound = null,
    };

    private readonly ApplicationDbContext _db;
    private readonly IMovieLensCsvReader _csvReader;
    private readonly IMovieLensUserSeeder _userSeeder;
    private readonly SeedingSettings _settings;
    private readonly ILogger<MovieLensRatingBackfillSeeder> _logger;

    public MovieLensRatingBackfillSeeder(
        ApplicationDbContext db,
        IMovieLensCsvReader csvReader,
        IMovieLensUserSeeder userSeeder,
        IOptions<SeedingSettings> settings,
        ILogger<MovieLensRatingBackfillSeeder> logger)
    {
        _db = db;
        _csvReader = csvReader;
        _userSeeder = userSeeder;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var paths = ResolveDataPaths();
        if (paths is null)
        {
            return;
        }

        var records = _csvReader.Read(paths.MoviesPath, paths.LinksPath);
        var movieMap = await BuildMovieMapAsync(records, cancellationToken);
        if (movieMap.Count == 0)
        {
            _logger.LogWarning("Rating backfill: no seeded movies link back to MovieLens; nothing to do");
            return;
        }

        var userMap = await _userSeeder.SeedAsync(_settings.MovieLensUserCount, cancellationToken);
        if (userMap.Count == 0)
        {
            _logger.LogWarning("Rating backfill: no MovieLens users available; nothing to do");
            return;
        }

        var existingPairs = await LoadExistingRatingPairsAsync(userMap, cancellationToken);

        _logger.LogInformation(
            "Rating backfill: scanning dataset for ratings missing from the DB " +
            "({Movies} movies mapped, {Users} users, {Existing} ratings already present)",
            movieMap.Count, userMap.Count, existingPairs.Count);

        var now = DateTime.UtcNow;
        using var reader = new StreamReader(paths.RatingsPath);
        using var csv = new CsvReader(reader, CsvConfiguration);

        var buffer = new List<MovieRating>(SeedingConstants.RatingsBatchSize);
        var touched = new HashSet<Guid>();
        var inserted = 0;
        var skippedExisting = 0;
        var rowsRead = 0L;

        await foreach (var row in csv.GetRecordsAsync<MovieLensRatingRow>(cancellationToken))
        {
            rowsRead++;

            if (!userMap.TryGetValue(row.UserId, out var dbUserId)
                || !movieMap.TryGetValue(row.MovieId, out var dbMovieId))
            {
                continue;
            }

            if (!existingPairs.Add((dbUserId, dbMovieId)))
            {
                skippedExisting++;
                continue;
            }

            buffer.Add(new MovieRating
            {
                UserId = dbUserId,
                MovieId = dbMovieId,
                Score = (decimal)row.Rating * 2m,
                CreatedAt = now,
                UpdatedAt = now,
            });
            touched.Add(dbMovieId);

            if (buffer.Count >= SeedingConstants.RatingsBatchSize)
            {
                inserted += await FlushAsync(buffer, cancellationToken);
                if (inserted % (SeedingConstants.RatingsBatchSize * SeedingConstants.ProgressLogInterval) == 0)
                {
                    _logger.LogInformation("Rating backfill: inserted {Inserted} ratings (scanned {Rows} rows)", inserted, rowsRead);
                }
            }
        }

        if (buffer.Count > 0)
        {
            inserted += await FlushAsync(buffer, cancellationToken);
        }

        await RecomputeAggregatesAsync(touched, cancellationToken);

        _logger.LogInformation(
            "Rating backfill complete: inserted {Inserted} new ratings across {Touched} movies " +
            "(skipped {Skipped} already present, scanned {Rows} rows)",
            inserted, touched.Count, skippedExisting, rowsRead);
    }

    private async Task<HashSet<(Guid UserId, Guid MovieId)>> LoadExistingRatingPairsAsync(
        IReadOnlyDictionary<int, Guid> userMap,
        CancellationToken cancellationToken)
    {
        var userIds = userMap.Values.ToHashSet();

        var pairs = await _db.Ratings
            .AsNoTracking()
            .Where(r => userIds.Contains(r.UserId))
            .Select(r => new { r.UserId, r.MovieId })
            .ToListAsync(cancellationToken);

        var set = new HashSet<(Guid, Guid)>(pairs.Count);
        foreach (var pair in pairs)
        {
            set.Add((pair.UserId, pair.MovieId));
        }

        return set;
    }

    private async Task<int> FlushAsync(List<MovieRating> buffer, CancellationToken cancellationToken)
    {
        await _db.Ratings.AddRangeAsync(buffer, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var count = buffer.Count;
        foreach (var entry in _db.ChangeTracker.Entries<MovieRating>().ToList())
        {
            entry.State = EntityState.Detached;
        }
        buffer.Clear();
        return count;
    }

    private async Task<IReadOnlyDictionary<int, Guid>> BuildMovieMapAsync(
        IReadOnlyList<MovieLensRecord> records,
        CancellationToken cancellationToken)
    {
        var imdbToMovieLensId = new Dictionary<string, int>(records.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.ImdbId))
            {
                continue;
            }

            imdbToMovieLensId.TryAdd(record.ImdbId, record.MovieLensId);
        }

        if (imdbToMovieLensId.Count == 0)
        {
            return new Dictionary<int, Guid>();
        }

        var dbMovies = await _db.Movies
            .AsNoTracking()
            .Where(m => m.ImdbId != null)
            .Select(m => new { m.Id, m.ImdbId })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<int, Guid>(dbMovies.Count);
        foreach (var movie in dbMovies)
        {
            if (movie.ImdbId is not null && imdbToMovieLensId.TryGetValue(movie.ImdbId, out var mlensId))
            {
                map[mlensId] = movie.Id;
            }
        }

        return map;
    }

    private async Task RecomputeAggregatesAsync(HashSet<Guid> movieIds, CancellationToken cancellationToken)
    {
        if (movieIds.Count == 0)
        {
            return;
        }

        var aggregates = await _db.Ratings
            .Where(r => movieIds.Contains(r.MovieId))
            .GroupBy(r => r.MovieId)
            .Select(g => new { MovieId = g.Key, Avg = g.Average(r => r.Score), Cnt = g.Count() })
            .ToListAsync(cancellationToken);

        var lookup = aggregates.ToDictionary(a => a.MovieId);
        var movies = await _db.Movies
            .Where(m => movieIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var movie in movies)
        {
            if (lookup.TryGetValue(movie.Id, out var agg))
            {
                movie.AverageRating = agg.Avg;
                movie.RatingsCount = agg.Cnt;
                movie.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private DataFiles? ResolveDataPaths()
    {
        var dataDir = _settings.DataPath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", SeedingConstants.DefaultDataDirectory);

        var moviesPath = Path.Combine(dataDir, SeedingConstants.MoviesFileName);
        var linksPath = Path.Combine(dataDir, SeedingConstants.LinksFileName);
        var ratingsPath = Path.Combine(dataDir, SeedingConstants.RatingsFileName);

        if (!File.Exists(ratingsPath))
        {
            _logger.LogError("Rating backfill: ratings dataset missing at {Path}", ratingsPath);
            return null;
        }

        return new DataFiles(moviesPath, linksPath, ratingsPath);
    }

    private sealed record DataFiles(string MoviesPath, string LinksPath, string RatingsPath);

    private sealed class MovieLensRatingRow
    {
        [Name("userId")]
        public int UserId { get; set; }

        [Name("movieId")]
        public int MovieId { get; set; }

        [Name("rating")]
        public float Rating { get; set; }

        [Name("timestamp")]
        public long Timestamp { get; set; }
    }
}
