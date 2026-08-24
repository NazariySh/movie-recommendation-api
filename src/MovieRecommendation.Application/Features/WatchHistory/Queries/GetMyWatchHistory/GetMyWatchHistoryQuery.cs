using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.WatchHistory;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.WatchHistory.Queries.GetMyWatchHistory;

public record GetMyWatchHistoryQuery(Guid UserId, int PageNumber, int PageSize, string Lang)
    : IQuery<PagedList<WatchHistoryDto>>;
