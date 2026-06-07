using MediatR;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Commands.ImportMovieFromImdb;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;
using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class MovieSeeder : IMovieSeeder
{
    private readonly IMediator _mediator;
    private readonly IMovieRepository _movieRepository;
    private readonly ILogger<MovieSeeder> _logger;

    public MovieSeeder(IMediator mediator, IMovieRepository movieRepository, ILogger<MovieSeeder> logger)
    {
        _mediator = mediator;
        _movieRepository = movieRepository;
        _logger = logger;
    }

    public async Task SeedAsync(IReadOnlyList<MovieLensRecord> source, int maxMovies, CancellationToken cancellationToken = default)
    {
        var candidates = source
            .Where(HasValidImdbId)
            .Take(maxMovies)
            .ToList();

        var imdbIds = candidates
            .Select(c => c.ImdbId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var existingImdbIds = await _movieRepository.GetExistingImdbIdsAsync(imdbIds, cancellationToken);

        var pending = candidates.Where(c => !existingImdbIds.Contains(c.ImdbId!)).ToList();
        var alreadyInDb = candidates.Count - pending.Count;

        _logger.LogInformation(
            "Importing {Count} movies via TMDb enrichment ({AlreadyInDb} already in DB, skipped)...",
            pending.Count, alreadyInDb);

        var imported = 0;
        var skipped = 0;
        var missing = 0;
        var failed = 0;

        for (var index = 0; index < pending.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var record = pending[index];
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
                    index + 1, pending.Count, imported, skipped, missing, failed);
            }
        }

        _logger.LogInformation(
            "Movie seeding complete. Imported={Imported} Skipped={Skipped} AlreadyInDb={AlreadyInDb} Missing={Missing} Failed={Failed}",
            imported, skipped, alreadyInDb, missing, failed);
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
