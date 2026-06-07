using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Commands.ImportMovieFromImdb;

public class ImportMovieFromImdbCommandHandler : ICommandHandler<ImportMovieFromImdbCommand, Guid>
{
    private const int MaxTextLength = 255;
    private const int MaxSlugBaseLength = 250;

    private readonly IExternalMovieDataProvider _externalProvider;
    private readonly IMovieRepository _movieRepository;
    private readonly IMovieKeyGenerator _keyGenerator;
    private readonly IGenreRepository _genreRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IImageMirrorService _imageMirror;
    private readonly IEmbeddingService? _embeddingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<ImportMovieFromImdbCommandHandler> _logger;

    public ImportMovieFromImdbCommandHandler(
        IExternalMovieDataProvider externalProvider,
        IMovieRepository movieRepository,
        IMovieKeyGenerator keyGenerator,
        IGenreRepository genreRepository,
        IArtistRepository artistRepository,
        IImageMirrorService imageMirror,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<ImportMovieFromImdbCommandHandler> logger,
        IEmbeddingService? embeddingService = null)
    {
        _externalProvider = externalProvider;
        _movieRepository = movieRepository;
        _keyGenerator = keyGenerator;
        _genreRepository = genreRepository;
        _artistRepository = artistRepository;
        _imageMirror = imageMirror;
        _embeddingService = embeddingService;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> Handle(ImportMovieFromImdbCommand request, CancellationToken cancellationToken)
    {
        var imdbId = request.Model.ImdbId.Trim();

        var existing = await _movieRepository.GetTrackedByImdbIdAsync(imdbId, cancellationToken);

        if (existing is not null)
        {
            throw new AlreadyExistsException($"Movie with IMDb id {imdbId} already exists (id={existing.Id}).");
        }

        var external = await _externalProvider.FetchByImdbIdAsync(imdbId, cancellationToken);

        if (external is null)
        {
            throw new NotFoundException($"No external data found for IMDb id {imdbId}.");
        }

        var titleExists = await _movieRepository.AnyAsync(
            m => m.OriginalTitle == external.OriginalTitle,
            cancellationToken);

        if (titleExists)
        {
            throw new AlreadyExistsException(
                $"Movie with original title '{external.OriginalTitle}' already exists.");
        }

        var key = await _keyGenerator.GenerateUniqueAsync(
            requestedKey: null,
            fallbackSource: external.Title,
            excludeId: null,
            cancellationToken);

        var posterTask = _imageMirror.MirrorAsync(external.PosterUrl, $"posters/{key}", cancellationToken);
        var backdropTask = _imageMirror.MirrorAsync(external.BackdropUrl, $"backdrops/{key}", cancellationToken);
        await Task.WhenAll(posterTask, backdropTask);
        var posterUrl = posterTask.Result;
        var backdropUrl = backdropTask.Result;

        var movie = new Movie
        {
            Key = key,
            ImdbId = external.ImdbId,
            TmdbId = external.TmdbId,
            Type = external.Type,
            OriginalTitle = Truncate(external.OriginalTitle)!,
            OriginalLang = external.OriginalLang,
            ReleaseDate = external.ReleaseDate,
            Status = external.Status,
            Runtime = external.Runtime,
            IsOngoing = external.IsOngoing,
            PosterUrl = posterUrl,
            BackdropUrl = backdropUrl,
            TrailerYoutubeId = external.TrailerYoutubeId,
            Translations =
            [
                new MovieTranslation
                {
                    LanguageCode = "en",
                    Title = Truncate(external.Title)!,
                    Overview = external.Overview,
                    Tagline = external.Tagline,
                },
            ],
            MovieGenres = await ResolveGenresAsync(external.Genres, cancellationToken),
            MovieCasts = await ResolveCastAsync(external.Cast, cancellationToken),
            Seasons = BuildSeasons(external.Seasons),
        };

        _movieRepository.Add(movie);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryComputeEmbeddingAsync(movie.Id, cancellationToken);

        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);
        _logger.LogInformation("Imported movie {MovieId} from IMDb {ImdbId}", movie.Id, imdbId);

        return movie.Id;
    }

    private async Task<List<MovieGenre>> ResolveGenresAsync(IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (names.Count == 0)
        {
            return [];
        }

        var slugs = names.Select(Slugify.ToSlug).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
        if (slugs.Count == 0)
        {
            return [];
        }

        var existing = await _genreRepository.GetAllAsync<Genre>(
            g => slugs.Contains(g.Slug),
            cancellationToken);

        var existingBySlug = existing.ToDictionary(g => g.Slug, g => g.Id);
        var resolved = new List<MovieGenre>();

        foreach (var (name, slug) in names.Zip(names.Select(Slugify.ToSlug)))
        {
            if (string.IsNullOrEmpty(slug))
            {
                continue;
            }

            if (existingBySlug.TryGetValue(slug, out var id))
            {
                resolved.Add(new MovieGenre { GenreId = id });
            }
        }

        return resolved.GroupBy(g => g.GenreId).Select(g => g.First()).ToList();
    }

    private async Task<List<MovieCast>> ResolveCastAsync(IReadOnlyList<ExternalMovieCast> casts, CancellationToken cancellationToken)
    {
        var validEntries = casts
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .ToList();

        if (validEntries.Count == 0)
        {
            return [];
        }

        var imdbIds = validEntries
            .Where(c => !string.IsNullOrWhiteSpace(c.ImdbId))
            .Select(c => c.ImdbId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var names = validEntries
            .Select(c => c.Name.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingByImdb = await _artistRepository.GetByImdbIdsAsync(imdbIds, cancellationToken);
        var existingByName = await _artistRepository.GetByNamesAsync(names, cancellationToken);

        var personIdByEntry = new Dictionary<ExternalMovieCast, Guid>();
        var newPersons = new List<Person>();
        var newByName = new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase);
        var reservedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in validEntries)
        {
            var name = entry.Name.Trim();
            if (name.Length > MaxTextLength)
            {
                name = name[..MaxTextLength];
            }

            if (!string.IsNullOrWhiteSpace(entry.ImdbId)
                && existingByImdb.TryGetValue(entry.ImdbId, out var byImdb))
            {
                personIdByEntry[entry] = byImdb.Id;
                continue;
            }

            if (existingByName.TryGetValue(name, out var byName))
            {
                personIdByEntry[entry] = byName.Id;
                continue;
            }

            if (newByName.TryGetValue(name, out var pending))
            {
                personIdByEntry[entry] = pending.Id;
                continue;
            }

            var uniqueSlug = await GenerateUniquePersonSlugAsync(name, reservedSlugs, cancellationToken);
            reservedSlugs.Add(uniqueSlug);

            var person = new Person
            {
                Slug = uniqueSlug,
                Name = name,
                PhotoUrl = await _imageMirror.MirrorAsync(entry.PhotoUrl, $"artists/{uniqueSlug}", cancellationToken),
                ImdbId = entry.ImdbId,
                TmdbId = entry.TmdbId,
            };

            _artistRepository.Add(person);
            newPersons.Add(person);
            newByName[name] = person;
            personIdByEntry[entry] = person.Id;
        }

        if (newPersons.Count > 0)
        {
            await PersistNewPeopleAsync(newPersons, personIdByEntry, cancellationToken);
        }

        var seen = new HashSet<Guid>();
        var resolved = new List<MovieCast>(validEntries.Count);

        foreach (var entry in validEntries)
        {
            if (!personIdByEntry.TryGetValue(entry, out var personId) || !seen.Add(personId))
            {
                continue;
            }

            resolved.Add(new MovieCast
            {
                PersonId = personId,
                Role = entry.Role,
                Character = Truncate(entry.Character),
                CastOrder = entry.CastOrder,
            });
        }

        return resolved;
    }

    private async Task PersistNewPeopleAsync(
        List<Person> newPersons,
        Dictionary<ExternalMovieCast, Guid> personIdByEntry,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Cast insert hit a unique conflict; reconciling against existing people");
        }

        _artistRepository.DetachRange(newPersons);

        var names = newPersons
            .Select(p => p.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var imdbIds = newPersons
            .Where(p => !string.IsNullOrWhiteSpace(p.ImdbId))
            .Select(p => p.ImdbId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingByName = await _artistRepository.GetByNamesAsync(names, cancellationToken);
        var existingByImdb = await _artistRepository.GetByImdbIdsAsync(imdbIds, cancellationToken);

        var remap = new Dictionary<Guid, Guid>();
        var stillMissing = new List<Person>();

        foreach (var person in newPersons)
        {
            Person? existing = null;
            if (!string.IsNullOrWhiteSpace(person.ImdbId)
                && existingByImdb.TryGetValue(person.ImdbId, out var byImdb))
            {
                existing = byImdb;
            }
            else if (existingByName.TryGetValue(person.Name, out var byName))
            {
                existing = byName;
            }

            if (existing is not null)
            {
                remap[person.Id] = existing.Id;
            }
            else
            {
                stillMissing.Add(person);
            }
        }

        foreach (var entry in personIdByEntry.Keys.ToList())
        {
            if (remap.TryGetValue(personIdByEntry[entry], out var existingId))
            {
                personIdByEntry[entry] = existingId;
            }
        }

        if (stillMissing.Count > 0)
        {
            foreach (var person in stillMissing)
            {
                _artistRepository.Add(person);
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _artistRepository.DetachRange(stillMissing);
                throw;
            }
        }
    }

    private static string? Truncate(string? value, int maxLength = MaxTextLength)
        => value is not null && value.Length > maxLength ? value[..maxLength] : value;

    private static List<Season> BuildSeasons(IReadOnlyList<ExternalMovieSeason> seasons)
    {
        return seasons
            .GroupBy(s => s.SeasonNumber)
            .Select(g => g.First())
            .Select(s => new Season
            {
                SeasonNumber = s.SeasonNumber,
                Name = Truncate(s.Name),
                Overview = s.Overview,
                PosterUrl = s.PosterUrl,
                EpisodeCount = s.EpisodeCount,
                AirDate = s.AirDate,
                VoteAverage = s.VoteAverage,
            })
            .ToList();
    }

    private async Task<string> GenerateUniquePersonSlugAsync(
        string name,
        HashSet<string> reservedSlugs,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slugify.ToSlug(name);
        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = $"person-{Guid.NewGuid().ToString()[..8]}";
        }

        if (baseSlug.Length > MaxSlugBaseLength)
        {
            baseSlug = baseSlug[..MaxSlugBaseLength];
        }

        var slug = baseSlug;
        var suffix = 2;
        while (reservedSlugs.Contains(slug)
            || await _artistRepository.SlugExistsAsync(slug, excludeId: null, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private async Task TryComputeEmbeddingAsync(Guid movieId, CancellationToken cancellationToken)
    {
        if (_embeddingService is null)
        {
            return;
        }

        var loaded = await _movieRepository.GetForEmbeddingAsync(movieId, cancellationToken);
        if (loaded is null)
        {
            return;
        }

        try
        {
            loaded.Embedding = await _embeddingService.GenerateMovieEmbeddingAsync(loaded);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding generation failed for movie {MovieId}; will retry later", movieId);
        }
    }
}
