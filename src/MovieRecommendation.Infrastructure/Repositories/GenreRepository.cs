using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Genres;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class GenreRepository : BaseRepository<Genre, int>, IGenreRepository
{
    public GenreRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public async Task<IReadOnlyList<GenreDto>> GetAllAsync(string lang, CancellationToken cancellationToken = default)
    {
        return await DbContext.Genres
            .AsNoTracking()
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.Slug)
            .ProjectTo<GenreDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);
    }

    public Task<Genre?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return DbContext.Genres
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Genres.Where(g => g.Slug == slug);

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasMoviesAsync(int genreId, CancellationToken cancellationToken = default)
    {
        return DbContext.Set<MovieGenre>().AnyAsync(mg => mg.GenreId == genreId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Genre>> GetBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default)
    {
        if (slugs.Count == 0)
        {
            return new Dictionary<string, Genre>();
        }

        var lowered = slugs.Select(s => s.ToLower()).ToList();

        var matches = await DbContext.Genres
            .Where(g => lowered.Contains(g.Slug) && !g.IsDeleted)
            .ToListAsync(cancellationToken);

        return matches.ToDictionary(g => g.Slug, g => g);
    }

    public async Task<IReadOnlyDictionary<int, Genre>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<int, Genre>();
        }

        var matches = await DbContext.Genres
            .Where(g => ids.Contains(g.Id) && !g.IsDeleted)
            .ToListAsync(cancellationToken);

        return matches.ToDictionary(g => g.Id, g => g);
    }
}
