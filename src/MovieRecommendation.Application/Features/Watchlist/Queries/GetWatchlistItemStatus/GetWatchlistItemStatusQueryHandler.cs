using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Watchlist.Queries.GetWatchlistItemStatus;

public class GetWatchlistItemStatusQueryHandler
    : IQueryHandler<GetWatchlistItemStatusQuery, WatchlistStatus?>
{
    private readonly IWatchlistRepository _watchlistRepository;

    public GetWatchlistItemStatusQueryHandler(IWatchlistRepository watchlistRepository)
    {
        _watchlistRepository = watchlistRepository;
    }

    public async Task<WatchlistStatus?> Handle(
        GetWatchlistItemStatusQuery request,
        CancellationToken cancellationToken)
    {
        var item = await _watchlistRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.MovieId,
            cancellationToken);

        return item?.Status;
    }
}
