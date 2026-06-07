using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class TranslationSeeder : ITranslationSeeder
{
    private const int MaxTitleLength = 255;
    private const int MaxOverviewLength = 2000;
    private const int MaxTaglineLength = 500;
    private const string TargetLanguage = LanguageCodes.Ukrainian;

    private readonly ApplicationDbContext _db;
    private readonly IExternalMovieDataProvider _provider;
    private readonly ILogger<TranslationSeeder> _logger;

    public TranslationSeeder(
        ApplicationDbContext db,
        IExternalMovieDataProvider provider,
        ILogger<TranslationSeeder> logger)
    {
        _db = db;
        _provider = provider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.Movies
            .Where(m => m.TmdbId != null
                && m.Translations.Any(t => t.LanguageCode == LanguageCodes.English)
                && !m.Translations.Any(t => t.LanguageCode == TargetLanguage))
            .Select(m => new PendingMovie(
                m.Id,
                m.TmdbId!.Value,
                m.Type,
                m.Translations
                    .Where(t => t.LanguageCode == LanguageCodes.English)
                    .Select(t => t.Title)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            _logger.LogInformation("Translation backfill: every movie already has a '{Lang}' translation", TargetLanguage);
            return;
        }

        _logger.LogInformation(
            "Translation backfill: fetching '{Lang}' translations for {Count} movies from TMDb...",
            TargetLanguage, pending.Count);

        var added = 0;
        var missing = 0;
        var failed = 0;
        var unsaved = 0;

        for (var index = 0; index < pending.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var movie = pending[index];

            try
            {
                var translation = await _provider.FetchTranslationAsync(
                    movie.TmdbId, movie.Type, TargetLanguage, cancellationToken);

                if (TryBuild(movie, translation, out var row))
                {
                    _db.MovieTranslations.Add(row);
                    added++;
                    unsaved++;
                }
                else
                {
                    missing++;
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Translation backfill failed for movie {MovieId} (TMDb {TmdbId})", movie.Id, movie.TmdbId);
            }

            if (unsaved >= SeedingConstants.TranslationBatchSize)
            {
                await _db.SaveChangesAsync(cancellationToken);
                unsaved = 0;
            }

            if ((index + 1) % SeedingConstants.ProgressLogInterval == 0)
            {
                _logger.LogInformation(
                    "Translation backfill progress: {Done}/{Total} (added={Added} missing={Missing} failed={Failed})",
                    index + 1, pending.Count, added, missing, failed);
            }
        }

        if (unsaved > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Translation backfill complete. Added={Added} Missing={Missing} Failed={Failed}",
            added, missing, failed);
    }

    private static bool TryBuild(PendingMovie movie, ExternalTranslationResult? translation, out MovieTranslation row)
    {
        row = null!;

        var title = translation?.Title;
        var overview = translation?.Overview;
        var tagline = translation?.Tagline;

        // Nothing localized at all — leave the movie to fall back to its English translation.
        if (string.IsNullOrWhiteSpace(title)
            && string.IsNullOrWhiteSpace(overview)
            && string.IsNullOrWhiteSpace(tagline))
        {
            return false;
        }

        // Title is required (NOT NULL). If TMDb has no Ukrainian title but does have a
        // localized overview/tagline, keep the English title so the synopsis is still localized.
        var resolvedTitle = string.IsNullOrWhiteSpace(title) ? movie.EnglishTitle : title;
        if (string.IsNullOrWhiteSpace(resolvedTitle))
        {
            return false;
        }

        row = new MovieTranslation
        {
            MovieId = movie.Id,
            LanguageCode = TargetLanguage,
            Title = Truncate(resolvedTitle!, MaxTitleLength)!,
            Overview = Truncate(overview, MaxOverviewLength),
            Tagline = Truncate(tagline, MaxTaglineLength),
        };

        return true;
    }

    private static string? Truncate(string? value, int maxLength)
        => value is not null && value.Length > maxLength ? value[..maxLength] : value;

    private sealed record PendingMovie(Guid Id, int TmdbId, TitleType Type, string? EnglishTitle);
}
