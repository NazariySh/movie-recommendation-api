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
    private readonly IExternalMovieDataProvider _externalProvider;
    private readonly IMovieRepository _movieRepository;
    private readonly IMovieKeyGenerator _keyGenerator;
    private readonly IGenreRepository _genreRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IBlobStorageService _blobStorage;
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
        IBlobStorageService blobStorage,
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
        _blobStorage = blobStorage;
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

        var key = await _keyGenerator.GenerateUniqueAsync(
            requestedKey: null,
            fallbackSource: external.Title,
            excludeId: null,
            cancellationToken);

        var posterTask = MirrorAsync(external.PosterUrl, $"posters/{key}", cancellationToken);
        var backdropTask = MirrorAsync(external.BackdropUrl, $"backdrops/{key}", cancellationToken);
        await Task.WhenAll(posterTask, backdropTask);
        var posterUrl = posterTask.Result;
        var backdropUrl = backdropTask.Result;

        var movie = new Movie
        {
            Key = key,
            ImdbId = external.ImdbId,
            TmdbId = external.TmdbId,
            Type = external.Type,
            OriginalTitle = external.OriginalTitle,
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
                    Title = external.Title,
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

    private async Task<string?> MirrorAsync(string? sourceUrl, string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return null;
        }

        try
        {
            var ext = Path.GetExtension(new Uri(sourceUrl).AbsolutePath);
            if (string.IsNullOrEmpty(ext))
            {
                ext = ".jpg";
            }

            return await _blobStorage.CopyFromUrlAsync(sourceUrl, path + ext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mirror image {SourceUrl}; falling back to source URL", sourceUrl);
            return sourceUrl;
        }
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

        var existingByImdb = await _artistRepository.GetByImdbIdsAsync(imdbIds, cancellationToken);

        var slugLookup = validEntries
            .Where(c => string.IsNullOrWhiteSpace(c.ImdbId) || !existingByImdb.ContainsKey(c.ImdbId!))
            .Select(c => Slugify.ToSlug(c.Name))
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingBySlug = await _artistRepository.GetBySlugsAsync(slugLookup, cancellationToken);

        var personIdByEntry = new Dictionary<ExternalMovieCast, Guid>();
        var newPersons = new List<Person>();
        var reservedSlugs = new HashSet<string>(existingBySlug.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in validEntries)
        {
            if (!string.IsNullOrWhiteSpace(entry.ImdbId)
                && existingByImdb.TryGetValue(entry.ImdbId, out var byImdb))
            {
                personIdByEntry[entry] = byImdb.Id;
                continue;
            }

            var slug = Slugify.ToSlug(entry.Name);
            if (!string.IsNullOrEmpty(slug)
                && existingBySlug.TryGetValue(slug, out var bySlug))
            {
                personIdByEntry[entry] = bySlug.Id;
                continue;
            }

            var existingNew = newPersons.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(entry.ImdbId) && p.ImdbId == entry.ImdbId ||
                !string.IsNullOrEmpty(slug) && p.Slug == slug);

            if (existingNew is not null)
            {
                personIdByEntry[entry] = existingNew.Id;
                continue;
            }

            var uniqueSlug = await GenerateUniquePersonSlugAsync(entry.Name, reservedSlugs, cancellationToken);
            reservedSlugs.Add(uniqueSlug);

            var person = new Person
            {
                Slug = uniqueSlug,
                Name = entry.Name,
                PhotoUrl = entry.PhotoUrl,
                ImdbId = entry.ImdbId,
                TmdbId = entry.TmdbId,
            };

            _artistRepository.Add(person);
            newPersons.Add(person);
            personIdByEntry[entry] = person.Id;
        }

        if (newPersons.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
                Character = entry.Character,
                CastOrder = entry.CastOrder,
            });
        }

        return resolved;
    }

    private static List<Season> BuildSeasons(IReadOnlyList<ExternalMovieSeason> seasons)
    {
        return seasons
            .GroupBy(s => s.SeasonNumber)
            .Select(g => g.First())
            .Select(s => new Season
            {
                SeasonNumber = s.SeasonNumber,
                Name = s.Name,
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
