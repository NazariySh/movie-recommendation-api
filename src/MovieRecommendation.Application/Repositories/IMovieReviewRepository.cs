using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IMovieReviewRepository : IRepository<MovieReview>
{
    Task<PagedList<MovieReviewDto>> GetForMovieAsync(
        Guid movieId,
        SearchReviewsDto query,
        Guid viewerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieReviewDto>> GetRepliesAsync(
        Guid reviewId,
        Guid viewerId,
        CancellationToken cancellationToken = default);

    Task<MovieReview?> GetTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountSubmittedTodayAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasHelpfulVoteAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default);

    void AddHelpfulVote(Guid userId, Guid reviewId);

    Task RemoveHelpfulVoteAsync(Guid userId, Guid reviewId, CancellationToken cancellationToken = default);
}
