using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class ArtistRepository : BaseRepository<Person>, IArtistRepository
{
    private const string FallbackLang = LanguageCodes.Default;
    private const double TrigramSimilarityThreshold = 0.3;

    public ArtistRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public async Task<PagedList<ArtistDto>> GetAllPaginatedAsync(
        SearchArtistsDto searchDto,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.People.AsNoTracking().Where(p => !p.IsDeleted);

        var search = searchDto.Search?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        if (hasSearch)
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                EF.Functions.TrigramsSimilarity(p.Name, search!) >= TrigramSimilarityThreshold);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Role))
        {
            var role = searchDto.Role.Trim().ToLower();
            query = query.Where(p =>
                DbContext.MovieCasts.Any(mc => mc.PersonId == p.Id && mc.Role.ToLower() == role));
        }

        var projected = query.Select(p => new
        {
            Person = p,
            MovieCount = DbContext.MovieCasts.Count(mc => mc.PersonId == p.Id),
            Roles = DbContext.MovieCasts
                .Where(mc => mc.PersonId == p.Id)
                .Select(mc => mc.Role)
                .Distinct()
                .ToList(),
            Similarity = hasSearch
                ? EF.Functions.TrigramsSimilarity(p.Name, search!)
                : (double?)null,
        });

        var sort = (searchDto.SortBy ?? "popular").Trim().ToLower();
        projected = sort switch
        {
            "name" => projected.OrderBy(x => x.Person.Name),
            "recent" => projected.OrderByDescending(x => x.Person.CreatedAt),
            _ when hasSearch => projected
                .OrderByDescending(x => x.Similarity)
                .ThenByDescending(x => x.MovieCount),
            _ => projected.OrderByDescending(x => x.MovieCount),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await projected
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .Select(x => new
            {
                x.Person.Id,
                x.Person.Slug,
                x.Person.Name,
                x.Person.PhotoUrl,
                x.Person.KnownForDepartment,
                x.MovieCount,
                Roles = x.Roles.ToList(),
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new ArtistDto
        {
            Id = r.Id,
            Slug = r.Slug,
            Name = r.Name,
            PhotoUrl = r.PhotoUrl,
            KnownForDepartment = r.KnownForDepartment,
            MovieCount = r.MovieCount,
            Roles = r.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.ToLower())
                .Distinct()
                .OrderBy(role => role)
                .ToList(),
        }).ToList();

        return new PagedList<ArtistDto>(items, searchDto.PageNumber, searchDto.PageSize, totalCount);
    }

    public async Task<ArtistDetailDto?> GetDetailByIdAsync(
        Guid id,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var person = await DbContext.People
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (person is null)
        {
            return null;
        }

        var roles = await DbContext.MovieCasts
            .AsNoTracking()
            .Where(mc => mc.PersonId == id)
            .Select(mc => mc.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

        var filmographyItems = await GetFilmographyAsync(id, roleFilter: null, lang, cancellationToken);

        var grouped = filmographyItems
            .GroupBy(x => x.Role.ToLower())
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<FilmographyItemDto>)g
                    .OrderByDescending(item => item.ReleaseDate ?? DateTime.MinValue)
                    .ToList());

        return new ArtistDetailDto
        {
            Id = person.Id,
            Slug = person.Slug,
            Name = person.Name,
            PhotoUrl = person.PhotoUrl,
            Birthday = person.Birthday,
            DateOfDeath = person.DateOfDeath,
            PlaceOfBirth = person.PlaceOfBirth,
            Nationality = person.Nationality,
            Gender = person.Gender,
            KnownForDepartment = person.KnownForDepartment,
            Biography = person.Biography,
            ImdbId = person.ImdbId,
            TmdbId = person.TmdbId,
            Roles = roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.ToLower())
                .Distinct()
                .OrderBy(role => role)
                .ToList(),
            Filmography = new FilmographyDto { ByRole = grouped },
        };
    }

    public async Task<IReadOnlyList<FilmographyItemDto>> GetFilmographyAsync(
        Guid artistId,
        string? roleFilter,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.MovieCasts
            .AsNoTracking()
            .Where(mc => mc.PersonId == artistId);

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            var role = roleFilter.Trim().ToLower();
            query = query.Where(mc => mc.Role.ToLower() == role);
        }

        return await query
            .Select(mc => new FilmographyItemDto
            {
                MovieId = mc.MovieId,
                MovieKey = mc.Movie.Key,
                MovieTitle =
                    mc.Movie.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                    ?? mc.Movie.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                    ?? mc.Movie.OriginalTitle,
                MovieType = mc.Movie.Type,
                ReleaseDate = mc.Movie.ReleaseDate,
                PosterUrl = mc.Movie.PosterUrl,
                Role = mc.Role,
                Character = mc.Character,
                CastOrder = mc.CastOrder,
                Rating = mc.Movie.Ratings.Any() ? mc.Movie.Ratings.Average(r => r.Score) : 0m,
                Genres = mc.Movie.MovieGenres.Select(mg =>
                    mg.Genre.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Slug).ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArtistDto>> GetPopularAsync(int count, CancellationToken cancellationToken = default)
    {
        var rows = await DbContext.People
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.Name,
                p.PhotoUrl,
                p.KnownForDepartment,
                MovieCount = DbContext.MovieCasts.Count(mc => mc.PersonId == p.Id),
                Roles = DbContext.MovieCasts
                    .Where(mc => mc.PersonId == p.Id)
                    .Select(mc => mc.Role)
                    .Distinct()
                    .ToList(),
            })
            .Where(x => x.MovieCount > 0)
            .OrderByDescending(x => x.MovieCount)
            .Take(count)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ArtistDto
        {
            Id = r.Id,
            Slug = r.Slug,
            Name = r.Name,
            PhotoUrl = r.PhotoUrl,
            KnownForDepartment = r.KnownForDepartment,
            MovieCount = r.MovieCount,
            Roles = r.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.ToLower())
                .Distinct()
                .OrderBy(role => role)
                .ToList(),
        }).ToList();
    }

    public Task<bool> HasMovieCastAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.MovieCasts.AnyAsync(mc => mc.PersonId == id, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbContext.People.Where(p => p.Slug == slug && !p.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<Person?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        return DbContext.People
            .AsNoTracking()
            .Where(p => p.ImdbId == imdbId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Person>> GetByImdbIdsAsync(
        IReadOnlyCollection<string> imdbIds,
        CancellationToken cancellationToken = default)
    {
        if (imdbIds.Count == 0)
        {
            return new Dictionary<string, Person>();
        }

        var rows = await DbContext.People
            .AsNoTracking()
            .Where(p => imdbIds.Contains(p.ImdbId!) && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows
            .Where(p => !string.IsNullOrEmpty(p.ImdbId))
            .GroupBy(p => p.ImdbId!)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IReadOnlyDictionary<string, Person>> GetBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default)
    {
        if (slugs.Count == 0)
        {
            return new Dictionary<string, Person>();
        }

        var rows = await DbContext.People
            .AsNoTracking()
            .Where(p => slugs.Contains(p.Slug) && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(p => p.Slug)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public Task<Person?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.People
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArtistSuggestionDto>> SearchSuggestionsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        return await DbContext.People
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(p =>
                EF.Functions.ILike(p.Name, $"%{trimmed}%") ||
                EF.Functions.TrigramsSimilarity(p.Name, trimmed) >= TrigramSimilarityThreshold)
            .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, trimmed))
            .Take(limit)
            .Select(p => new ArtistSuggestionDto
            {
                Id = p.Id,
                Slug = p.Slug,
                Name = p.Name,
                PhotoUrl = p.PhotoUrl,
                KnownFor = p.KnownForDepartment,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountSearchMatchesAsync(string query, CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Task.FromResult(0);
        }

        return DbContext.People
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(p =>
                EF.Functions.ILike(p.Name, $"%{trimmed}%") ||
                EF.Functions.TrigramsSimilarity(p.Name, trimmed) >= TrigramSimilarityThreshold)
            .CountAsync(cancellationToken);
    }
}
