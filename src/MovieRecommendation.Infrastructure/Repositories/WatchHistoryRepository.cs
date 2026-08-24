using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.WatchHistory;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class WatchHistoryRepository : BaseRepository<WatchHistory>, IWatchHistoryRepository
{
    private const int MaxPageSize = 100;

    public WatchHistoryRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public Task<WatchHistory?> GetByUserAndMovieAsync(Guid userId, Guid movieId, CancellationToken cancellationToken = default)
    {
        return DbContext.WatchHistory
            .FirstOrDefaultAsync(h => h.UserId == userId && h.MovieId == movieId, cancellationToken);
    }

    public async Task<PagedList<WatchHistoryDto>> GetForUserAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        string lang,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, MaxPageSize);
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;

        var query = DbContext.WatchHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.WatchedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<WatchHistoryDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);

        return new PagedList<WatchHistoryDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task UpsertAsync(Guid userId, Guid movieId, DateTime watchedAt, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserAndMovieAsync(userId, movieId, cancellationToken);

        if (existing is null)
        {
            DbContext.WatchHistory.Add(new WatchHistory
            {
                UserId = userId,
                MovieId = movieId,
                WatchedAt = watchedAt,
            });
            return;
        }

        existing.WatchedAt = watchedAt;
        existing.UpdatedAt = DateTime.UtcNow;
    }
}
