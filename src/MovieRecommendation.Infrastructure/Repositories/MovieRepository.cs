using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;
using Npgsql;

namespace MovieRecommendation.Infrastructure.Repositories;

public class MovieRepository : BaseRepository<Movie>, IMovieRepository
{
    private const string DefaultSortingOption = "popularity";
    private const string FallbackLang = LanguageCodes.Default;
    private const int MaxPageSize = 100;

    private static readonly Dictionary<string, Expression<Func<Movie, object?>>> SortingOptions = new()
    {
        { DefaultSortingOption, x => x.ReleaseDate! },
        { "newest", x => x.CreatedAt },
        { "rating", x => x.Ratings.Any() ? x.Ratings.Average(r => r.Score) : 0m },
        { "release_date", x => x.ReleaseDate! },
        { "releasedate", x => x.ReleaseDate! },
        { "title", x => x.OriginalTitle },
    };

    public MovieRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public async Task<PagedList<MovieListItemDto>> GetAllPaginatedAsync(
        SearchMoviesDto searchDto,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(searchDto.PageSize <= 0 ? 20 : searchDto.PageSize, 1, MaxPageSize);
        var pageNumber = searchDto.PageNumber <= 0 ? 1 : searchDto.PageNumber;

        var query = DbContext.Movies.AsNoTracking().Where(x => !x.IsDeleted);

        query = ApplyFilters(query, searchDto, lang);
        query = ApplySort(query, searchDto.SortBy, searchDto.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<MovieListItemDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);

        return new PagedList<MovieListItemDto>(items, pageNumber, pageSize, totalCount);
    }

    public Task<MovieDetailDto?> GetDetailByIdAsync(Guid id, string lang, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .ProjectTo<MovieDetailDto>(MapperConfiguration, new { lang, currentUserId })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<MovieDetailDto?> GetDetailByKeyAsync(string key, string lang, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .AsNoTracking()
            .Where(x => x.Key == key && !x.IsDeleted)
            .ProjectTo<MovieDetailDto>(MapperConfiguration, new { lang, currentUserId })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<MovieDetailDto?> GetDetailByImdbIdAsync(string imdbId, string lang, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .AsNoTracking()
            .Where(x => x.ImdbId == imdbId && !x.IsDeleted)
            .ProjectTo<MovieDetailDto>(MapperConfiguration, new { lang, currentUserId })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovieDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        string lang,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await DbContext.Movies
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ProjectTo<MovieDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovieListItemDto>> GetListItemsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        string lang,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await DbContext.Movies
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ProjectTo<MovieListItemDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovieListItemDto>> GetSimilarAsync(
        Guid movieId,
        int count,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var seed = await DbContext.Movies
            .AsNoTracking()
            .Where(x => x.Id == movieId && !x.IsDeleted)
            .Select(x => new
            {
                x.Type,
                GenreIds = x.MovieGenres.Select(mg => mg.GenreId).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (seed is null)
        {
            return [];
        }

        return await DbContext.Movies
            .AsNoTracking()
            .Where(x => x.Id != movieId && !x.IsDeleted && x.Type == seed.Type)
            .Where(x => x.MovieGenres.Any(mg => seed.GenreIds.Contains(mg.GenreId)))
            .OrderByDescending(x => x.MovieGenres.Count(mg => seed.GenreIds.Contains(mg.GenreId)))
            .ThenByDescending(x => x.Ratings.Any() ? x.Ratings.Average(r => r.Score) : 0m)
            .Take(count)
            .ProjectTo<MovieListItemDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);
    }

    public Task<Movie?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .Include(x => x.Translations)
            .Include(x => x.MovieGenres)
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Movie?> GetTrackedByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .Where(x => x.ImdbId == imdbId && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Movie?> GetForEmbeddingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .Include(x => x.Translations)
            .Include(x => x.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .ThenInclude(g => g.Translations)
            .Include(x => x.Keywords)
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Movie?> GetForAdminEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.Movies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.MovieGenres)
            .Include(x => x.Seasons)
            .Where(x => x.Id == id && !x.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> KeyExistsAsync(string key, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Movies.Where(x => x.Key == key && !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetTrendingIdsAsync(
        TitleType type,
        int daysWindow,
        double halfLifeDays,
        double ratingCentre,
        int minRatings,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT m.id
            FROM movies m
            JOIN movie_ratings r ON m.id = r.movie_id
            WHERE r.created_at > NOW() - (@daysWindow || ' days')::interval
              AND m.type = @type
              AND m.is_deleted = false
            GROUP BY m.id
            HAVING COUNT(*) >= @minRatings
            ORDER BY SUM(
                EXP(-EXTRACT(EPOCH FROM (NOW() - r.created_at)) / 86400.0 / @halfLife)
                * (CAST(r.score AS double precision) - @centre)
            ) DESC
            LIMIT @limit;
            """;

        var conn = DbContext.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await conn.OpenAsync(cancellationToken);
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter("@type", type.ToString()));
            cmd.Parameters.Add(new NpgsqlParameter("@daysWindow", daysWindow));
            cmd.Parameters.Add(new NpgsqlParameter("@halfLife", halfLifeDays));
            cmd.Parameters.Add(new NpgsqlParameter("@centre", ratingCentre));
            cmd.Parameters.Add(new NpgsqlParameter("@minRatings", minRatings));
            cmd.Parameters.Add(new NpgsqlParameter("@limit", limit));

            var ids = new List<Guid>(limit);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ids.Add(reader.GetGuid(0));
            }
            return ids;
        }
        finally
        {
            if (shouldClose)
            {
                await conn.CloseAsync();
            }
        }
    }

    public async Task<IReadOnlyList<MovieSuggestionDto>> SearchSuggestionsAsync(
        string query,
        TitleType? type,
        int limit,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        var q = DbContext.Movies
            .AsNoTracking()
            .Where(m => !m.IsDeleted);

        if (type.HasValue)
        {
            q = q.Where(m => m.Type == type.Value);
        }

        q = q.Where(m =>
            m.OriginalTitle.ToLower().Contains(trimmed) ||
            m.Translations.Any(t =>
                (t.LanguageCode == lang || t.LanguageCode == FallbackLang) &&
                t.Title.ToLower().Contains(trimmed)));

        return await q
            .OrderByDescending(m => m.OriginalTitle.ToLower().StartsWith(trimmed))
            .ThenByDescending(m => m.RatingsCount)
            .Take(limit)
            .Select(m => new MovieSuggestionDto
            {
                Id = m.Id,
                Key = m.Key,
                Type = m.Type,
                Title = m.Translations
                    .Where(t => t.LanguageCode == lang || t.LanguageCode == FallbackLang)
                    .OrderByDescending(t => t.LanguageCode == lang)
                    .Select(t => t.Title)
                    .FirstOrDefault() ?? m.OriginalTitle,
                ReleaseYear = m.ReleaseDate.HasValue ? m.ReleaseDate.Value.Year : (int?)null,
                PosterUrl = m.PosterUrl,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovieSuggestionDto>> GetSuggestionsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        string lang,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await DbContext.Movies
            .AsNoTracking()
            .Where(m => ids.Contains(m.Id) && !m.IsDeleted)
            .Select(m => new MovieSuggestionDto
            {
                Id = m.Id,
                Key = m.Key,
                Type = m.Type,
                Title = m.Translations
                    .Where(t => t.LanguageCode == lang || t.LanguageCode == FallbackLang)
                    .OrderByDescending(t => t.LanguageCode == lang)
                    .Select(t => t.Title)
                    .FirstOrDefault() ?? m.OriginalTitle,
                ReleaseYear = m.ReleaseDate.HasValue ? m.ReleaseDate.Value.Year : (int?)null,
                PosterUrl = m.PosterUrl,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountSearchMatchesAsync(
        string query,
        TitleType? type,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var trimmed = query.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Task.FromResult(0);
        }

        var q = DbContext.Movies
            .AsNoTracking()
            .Where(m => !m.IsDeleted);

        if (type.HasValue)
        {
            q = q.Where(m => m.Type == type.Value);
        }

        q = q.Where(m =>
            m.OriginalTitle.ToLower().Contains(trimmed) ||
            m.Translations.Any(t =>
                (t.LanguageCode == lang || t.LanguageCode == FallbackLang) &&
                t.Title.ToLower().Contains(trimmed)));

        return q.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetPopularIdsAsync(
        TitleType? type,
        string? genreSlug,
        int minRatings,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = DbContext.Movies
            .AsNoTracking()
            .Where(m => !m.IsDeleted && m.RatingsCount >= minRatings);

        if (type.HasValue)
        {
            query = query.Where(m => m.Type == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(genreSlug))
        {
            var slug = genreSlug.Trim().ToLower();
            query = query.Where(m => m.MovieGenres.Any(mg => mg.Genre.Slug == slug));
        }

        const decimal globalMean = 3.0m;
        decimal smoothing = minRatings;

        return await query
            .OrderByDescending(m =>
                ((decimal)m.RatingsCount * m.AverageRating + smoothing * globalMean) /
                ((decimal)m.RatingsCount + smoothing))
            .Take(limit)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);
    }


    private static IQueryable<Movie> ApplyFilters(IQueryable<Movie> query, SearchMoviesDto searchDto, string lang)
    {
        if (searchDto.Type is not null)
        {
            query = query.Where(x => x.Type == searchDto.Type);
        }

        var searchTerm = searchDto.Search?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.Key.ToLower().Contains(searchTerm) ||
                x.OriginalTitle.ToLower().Contains(searchTerm) ||
                x.Translations.Any(t =>
                    (t.LanguageCode == lang || t.LanguageCode == FallbackLang) &&
                    t.Title.ToLower().Contains(searchTerm)));
        }

        if (searchDto.Genres is { Count: > 0 })
        {
            query = query.Where(x => x.MovieGenres.Any(g => searchDto.Genres.Contains(g.GenreId)));
        }

        if (searchDto.GenreSlugs is { Count: > 0 })
        {
            var slugs = searchDto.GenreSlugs.Select(s => s.ToLower()).ToList();
            query = query.Where(x => x.MovieGenres.Any(g => slugs.Contains(g.Genre.Slug)));
        }

        if (searchDto.ReleaseYears is { Count: > 0 })
        {
            query = query.Where(x => x.ReleaseDate.HasValue && searchDto.ReleaseYears.Contains(x.ReleaseDate.Value.Year));
        }

        if (searchDto.YearFrom.HasValue)
        {
            query = query.Where(x => x.ReleaseDate.HasValue && x.ReleaseDate.Value.Year >= searchDto.YearFrom.Value);
        }

        if (searchDto.YearTo.HasValue)
        {
            query = query.Where(x => x.ReleaseDate.HasValue && x.ReleaseDate.Value.Year <= searchDto.YearTo.Value);
        }

        if (searchDto.MinRating.HasValue)
        {
            var min = searchDto.MinRating.Value;
            query = query.Where(x =>
                x.Ratings.Any() && x.Ratings.Average(r => r.Score) >= min);
        }

        if (searchDto.MinRatingsCount.HasValue && searchDto.MinRatingsCount.Value > 0)
        {
            var minCount = searchDto.MinRatingsCount.Value;
            query = query.Where(x => x.Ratings.Count() >= minCount);
        }

        if (searchDto.RuntimeMin.HasValue)
        {
            query = query.Where(x => x.Runtime.HasValue && x.Runtime.Value >= searchDto.RuntimeMin.Value);
        }

        if (searchDto.RuntimeMax.HasValue)
        {
            query = query.Where(x => x.Runtime.HasValue && x.Runtime.Value <= searchDto.RuntimeMax.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Language))
        {
            var langFilter = searchDto.Language.ToLower();
            query = query.Where(x => x.OriginalLang == langFilter);
        }

        return query;
    }

    private static IQueryable<Movie> ApplySort(IQueryable<Movie> query, string? sortBy, bool descending)
    {
        var key = (sortBy ?? DefaultSortingOption).Trim().ToLower();
        var sortExpression = SortingOptions.TryGetValue(key, out var expression)
            ? expression
            : SortingOptions[DefaultSortingOption];

        return descending
            ? query.OrderByDescending(sortExpression)
            : query.OrderBy(sortExpression);
    }
}
