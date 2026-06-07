using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Watchlist.Queries.GetWatchlistItemStatus;

public record GetWatchlistItemStatusQuery(Guid UserId, Guid MovieId) : IQuery<WatchlistStatus?>;
