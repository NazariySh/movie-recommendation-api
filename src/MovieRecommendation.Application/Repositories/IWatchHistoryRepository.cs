using MovieRecommendation.Application.DTOs.WatchHistory;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IWatchHistoryRepository : IRepository<WatchHistory>
{
    Task<WatchHistory?> GetByUserAndMovieAsync(Guid userId, Guid movieId, CancellationToken cancellationToken = default);

    Task<PagedList<WatchHistoryDto>> GetForUserAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        string lang,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(Guid userId, Guid movieId, DateTime watchedAt, CancellationToken cancellationToken = default);
}
