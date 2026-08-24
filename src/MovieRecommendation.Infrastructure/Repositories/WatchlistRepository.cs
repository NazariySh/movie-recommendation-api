using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class WatchlistRepository : BaseRepository<WatchlistItem>, IWatchlistRepository
{
    private const int MaxPageSize = 100;

    public WatchlistRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public Task<WatchlistItem?> GetByUserAndMovieAsync(Guid userId, Guid movieId, CancellationToken cancellationToken = default)
    {
        return DbContext.WatchlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId, cancellationToken);
    }

    public async Task<PagedList<WatchlistItemDto>> GetForUserAsync(
        Guid userId,
        SearchWatchlistDto query,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, MaxPageSize);
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

        var baseQuery = DbContext.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId && !w.IsDeleted);

        if (query.Status is not null)
        {
            baseQuery = baseQuery.Where(w => w.Status == query.Status);
        }

        baseQuery = baseQuery.OrderByDescending(w => w.UpdatedAt);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<WatchlistItemDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);

        return new PagedList<WatchlistItemDto>(items, pageNumber, pageSize, totalCount);
    }
}
