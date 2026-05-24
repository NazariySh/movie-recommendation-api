using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Watchlist.Queries.GetMyWatchlist;

public class GetMyWatchlistQueryHandler : IQueryHandler<GetMyWatchlistQuery, PagedList<WatchlistItemDto>>
{
    private readonly IWatchlistRepository _watchlistRepository;

    public GetMyWatchlistQueryHandler(IWatchlistRepository watchlistRepository)
    {
        _watchlistRepository = watchlistRepository;
    }

    public async Task<PagedList<WatchlistItemDto>> Handle(GetMyWatchlistQuery request, CancellationToken cancellationToken)
    {
        return await _watchlistRepository.GetForUserAsync(
            request.UserId,
            request.Query,
            request.Lang,
            cancellationToken);
    }
}
