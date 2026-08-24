using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class MovieReviewRepository : BaseRepository<MovieReview>, IMovieReviewRepository
{
    private const int MaxPageSize = 100;
    private const string DefaultSort = "hottest";

    public MovieReviewRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public async Task<PagedList<MovieReviewDto>> GetForMovieAsync(
        Guid movieId,
        SearchReviewsDto query,
        Guid viewerId,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 10 : query.PageSize, 1, MaxPageSize);
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;

        var baseQuery = DbContext.MovieReviews
            .AsNoTracking()
            .Where(r => r.MovieId == movieId
                && r.ParentReviewId == null
                && !r.IsDeleted);

        baseQuery = ApplySort(baseQuery, query.Sort);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<MovieReviewDto>(MapperConfiguration, new { viewerId })
            .ToListAsync(cancellationToken);

        return new PagedList<MovieReviewDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<MovieReviewDto>> GetRepliesAsync(
        Guid reviewId,
        Guid viewerId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.MovieReviews
            .AsNoTracking()
            .Where(r => r.ParentReviewId == reviewId
                && !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .ProjectTo<MovieReviewDto>(MapperConfiguration, new { viewerId })
            .ToListAsync(cancellationToken);
    }

    public Task<MovieReview?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext.MovieReviews
            .Include(r => r.Replies)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public Task<int> CountSubmittedTodayAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        return DbContext.MovieReviews
            .Where(r => r.UserId == userId
                && r.ParentReviewId == null
                && r.CreatedAt >= since)
            .CountAsync(cancellationToken);
    }

    public Task<bool> HasHelpfulVoteAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        return DbContext.ReviewHelpfulVotes
            .AnyAsync(v => v.UserId == userId && v.ReviewId == reviewId, cancellationToken);
    }

    public void AddHelpfulVote(Guid userId, Guid reviewId)
    {
        DbContext.ReviewHelpfulVotes.Add(new ReviewHelpfulVote
        {
            UserId = userId,
            ReviewId = reviewId,
        });
    }

    public async Task RemoveHelpfulVoteAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var vote = await DbContext.ReviewHelpfulVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ReviewId == reviewId, cancellationToken);

        if (vote is not null)
        {
            DbContext.ReviewHelpfulVotes.Remove(vote);
        }
    }

    private static IQueryable<MovieReview> ApplySort(IQueryable<MovieReview> query, string? sort)
    {
        return (sort ?? DefaultSort).ToLowerInvariant() switch
        {
            "newest" => query.OrderByDescending(r => r.CreatedAt),
            "oldest" => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt),
        };
    }
}
