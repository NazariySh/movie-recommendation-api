using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class GenreSeeder : IGenreSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GenreSeeder> _logger;

    public GenreSeeder(ApplicationDbContext db, ILogger<GenreSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingSlugs = await _db.Genres
            .Select(g => g.Slug)
            .ToListAsync(cancellationToken);

        var existingSlugSet = existingSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = CanonicalGenres.All
            .Where(g => !existingSlugSet.Contains(g.Slug))
            .Select(BuildGenre)
            .ToList();

        if (missing.Count == 0)
        {
            _logger.LogInformation("Genres already up to date ({Count} present)", existingSlugs.Count);
            return;
        }

        await _db.Genres.AddRangeAsync(missing, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} new genres", missing.Count);
    }

    private static Genre BuildGenre(CanonicalGenre canonical) => new()
    {
        Slug = canonical.Slug,
        Translations =
        [
            new GenreTranslation { LanguageCode = LanguageCodes.English, Name = canonical.EnglishName },
            new GenreTranslation { LanguageCode = LanguageCodes.Ukrainian, Name = canonical.UkrainianName },
        ],
    };
}
