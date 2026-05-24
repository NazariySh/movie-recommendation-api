using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Watchlist.Queries.GetMyWatchlist;

public record GetMyWatchlistQuery(Guid UserId, SearchWatchlistDto Query, string Lang)
    : IQuery<PagedList<WatchlistItemDto>>;
