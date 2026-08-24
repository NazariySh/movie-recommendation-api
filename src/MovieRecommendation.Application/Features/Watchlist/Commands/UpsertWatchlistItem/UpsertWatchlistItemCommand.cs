using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Watchlist;

namespace MovieRecommendation.Application.Features.Watchlist.Commands.UpsertWatchlistItem;

public record UpsertWatchlistItemCommand(Guid UserId, UpsertWatchlistDto Model) : ICommand;
