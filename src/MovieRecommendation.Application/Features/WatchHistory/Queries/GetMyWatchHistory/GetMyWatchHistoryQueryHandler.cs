using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.WatchHistory;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.WatchHistory.Queries.GetMyWatchHistory;

public class GetMyWatchHistoryQueryHandler : IQueryHandler<GetMyWatchHistoryQuery, PagedList<WatchHistoryDto>>
{
    private readonly IWatchHistoryRepository _watchHistoryRepository;

    public GetMyWatchHistoryQueryHandler(IWatchHistoryRepository watchHistoryRepository)
    {
        _watchHistoryRepository = watchHistoryRepository;
    }

    public async Task<PagedList<WatchHistoryDto>> Handle(GetMyWatchHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _watchHistoryRepository.GetForUserAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            request.Lang,
            cancellationToken);
    }
}
