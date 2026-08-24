using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IWatchlistRepository : IRepository<WatchlistItem>
{
    Task<WatchlistItem?> GetByUserAndMovieAsync(Guid userId, Guid movieId, CancellationToken cancellationToken = default);

    Task<PagedList<WatchlistItemDto>> GetForUserAsync(
        Guid userId,
        SearchWatchlistDto query,
        string lang,
        CancellationToken cancellationToken = default);
}
