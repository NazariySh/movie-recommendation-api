using MediatR;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Commands.ImportMovieFromImdb;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;
using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class MovieSeeder : IMovieSeeder
{
    private readonly IMediator _mediator;
    private readonly ILogger<MovieSeeder> _logger;

    public MovieSeeder(IMediator mediator, ILogger<MovieSeeder> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task SeedAsync(IReadOnlyList<MovieLensRecord> source, int maxMovies, CancellationToken cancellationToken = default)
    {
        var candidates = source
            .Where(HasValidImdbId)
            .Take(maxMovies)
            .ToList();

        _logger.LogInformation("Importing {Count} movies via TMDb enrichment...", candidates.Count);

        var imported = 0;
        var skipped = 0;
        var missing = 0;
        var failed = 0;

        for (var index = 0; index < candidates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = candidates[index];
            var outcome = await ImportSingleAsync(record, cancellationToken);

            switch (outcome)
            {
                case ImportOutcome.Imported: imported++; break;
                case ImportOutcome.AlreadyExists: skipped++; break;
                case ImportOutcome.Missing: missing++; break;
                case ImportOutcome.Failed: failed++; break;
            }

            if ((index + 1) % SeedingConstants.ProgressLogInterval == 0)
            {
                _logger.LogInformation(
                    "Progress: {Done}/{Total} (imported={I} skipped={S} missing={M} failed={F})",
                    index + 1, candidates.Count, imported, skipped, missing, failed);
            }
        }

        _logger.LogInformation(
            "Movie seeding complete. Imported={Imported} Skipped={Skipped} Missing={Missing} Failed={Failed}",
            imported, skipped, missing, failed);
    }

    private async Task<ImportOutcome> ImportSingleAsync(MovieLensRecord record, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(
                new ImportMovieFromImdbCommand(new ImportMovieFromImdbDto { ImdbId = record.ImdbId! }),
                cancellationToken);
            return ImportOutcome.Imported;
        }
        catch (AlreadyExistsException)
        {
            return ImportOutcome.AlreadyExists;
        }
        catch (NotFoundException)
        {
            _logger.LogDebug("No external data for IMDb {ImdbId} ({Title})", record.ImdbId, record.Title);
            return ImportOutcome.Missing;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed movie IMDb={ImdbId} Title={Title}", record.ImdbId, record.Title);
            return ImportOutcome.Failed;
        }
    }

    private static bool HasValidImdbId(MovieLensRecord record) =>
        !string.IsNullOrWhiteSpace(record.ImdbId);

    private enum ImportOutcome
    {
        Imported,
        AlreadyExists,
        Missing,
        Failed,
    }
}
