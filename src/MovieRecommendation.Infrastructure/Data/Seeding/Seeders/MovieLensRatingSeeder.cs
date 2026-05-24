using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;
using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class MovieLensRatingSeeder : IMovieLensRatingSeeder
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
    private readonly ILogger<MovieLensRatingSeeder> _logger;

    public MovieLensRatingSeeder(ApplicationDbContext db, ILogger<MovieLensRatingSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(
        string ratingsPath,
        IReadOnlyList<MovieLensRecord> records,
        IReadOnlyDictionary<int, Guid> movieLensUserIdToDbUserId,
        CancellationToken cancellationToken = default)
    {
        if (await _db.Ratings.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Ratings already present; skipping MovieLens rating seeding");
            return;
        }

        if (!File.Exists(ratingsPath))
        {
            _logger.LogError("Ratings dataset missing: {Path}", ratingsPath);
            return;
        }

        if (movieLensUserIdToDbUserId.Count == 0)
        {
            _logger.LogWarning("No MovieLens users mapped; skipping rating seeding");
            return;
        }

        var movieMap = await BuildMovieMapAsync(records, cancellationToken);
        if (movieMap.Count == 0)
        {
            _logger.LogWarning("No seeded movies link back to MovieLens; skipping rating seeding");
            return;
        }

        _logger.LogInformation(
            "Seeding MovieLens ratings: {UserCount} users x {MovieCount} movies",
            movieLensUserIdToDbUserId.Count,
            movieMap.Count);

        var minUserId = movieLensUserIdToDbUserId.Keys.Min();
        var maxUserId = movieLensUserIdToDbUserId.Keys.Max();
        var now = DateTime.UtcNow;

        using var reader = new StreamReader(ratingsPath);
        using var csv = new CsvReader(reader, CsvConfiguration);

        var buffer = new List<MovieRating>(SeedingConstants.RatingsBatchSize);
        var dedup = new HashSet<(Guid UserId, Guid MovieId)>();
        var touched = new HashSet<Guid>();
        var inserted = 0;
        var rowsRead = 0L;

        await foreach (var row in csv.GetRecordsAsync<MovieLensRatingRow>(cancellationToken))
        {
            rowsRead++;

            if (row.UserId > maxUserId)
            {
                break;
            }

            if (row.UserId < minUserId)
            {
                continue;
            }

            if (!movieLensUserIdToDbUserId.TryGetValue(row.UserId, out var dbUserId))
            {
                continue;
            }

            if (!movieMap.TryGetValue(row.MovieId, out var dbMovieId))
            {
                continue;
            }

            if (!dedup.Add((dbUserId, dbMovieId)))
            {
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
                    _logger.LogInformation("Inserted {Inserted} ratings (scanned {Rows} rows)", inserted, rowsRead);
                }
            }
        }

        if (buffer.Count > 0)
        {
            inserted += await FlushAsync(buffer, cancellationToken);
        }

        await RecomputeAggregatesAsync(touched, cancellationToken);

        _logger.LogInformation(
            "MovieLens ratings seeded: {Inserted} rows across {Touched} movies",
            inserted,
            touched.Count);
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
            if (movie.ImdbId is null)
            {
                continue;
            }

            if (imdbToMovieLensId.TryGetValue(movie.ImdbId, out var mlensId))
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
