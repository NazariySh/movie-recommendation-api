using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Watchlist.Commands.RemoveWatchlistItem;

public record RemoveWatchlistItemCommand(Guid UserId, Guid MovieId) : ICommand;
