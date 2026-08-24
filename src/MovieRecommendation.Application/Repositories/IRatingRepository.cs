using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IRatingRepository : IRepository<MovieRating>
{
    Task<MovieRating?> GetByUserAndMovieAsync(Guid userId, Guid movieId, CancellationToken cancellationToken = default);

    Task<PagedList<UserRatingDto>> GetUserRatingsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        string lang,
        CancellationToken cancellationToken = default);

    Task RecomputeAggregatesAsync(Guid movieId, CancellationToken cancellationToken = default);
}
