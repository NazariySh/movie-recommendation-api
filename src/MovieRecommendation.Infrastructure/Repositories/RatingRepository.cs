using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class RatingRepository : BaseRepository<MovieRating>, IRatingRepository
{
    private const int MaxPageSize = 100;

    public RatingRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public Task<MovieRating?> GetByUserAndMovieAsync(Guid userId, Guid movieId, CancellationToken cancellationToken = default)
    {
        return DbContext.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId, cancellationToken);
    }

    public async Task<PagedList<UserRatingDto>> GetUserRatingsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        string lang,
        CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, MaxPageSize);
        var number = pageNumber <= 0 ? 1 : pageNumber;

        var query = DbContext.Ratings
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((number - 1) * size)
            .Take(size)
            .ProjectTo<UserRatingDto>(MapperConfiguration, new { lang })
            .ToListAsync(cancellationToken);

        return new PagedList<UserRatingDto>(items, number, size, totalCount);
    }

    public async Task RecomputeAggregatesAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        var stats = await DbContext.Ratings
            .Where(r => r.MovieId == movieId)
            .GroupBy(r => r.MovieId)
            .Select(g => new { Avg = g.Average(r => r.Score), Cnt = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var movie = await DbContext.Movies.FirstOrDefaultAsync(m => m.Id == movieId, cancellationToken);

        if (movie is null)
        {
            return;
        }

        movie.AverageRating = stats?.Avg ?? 0m;
        movie.RatingsCount = stats?.Cnt ?? 0;
        movie.UpdatedAt = DateTime.UtcNow;
    }
}
