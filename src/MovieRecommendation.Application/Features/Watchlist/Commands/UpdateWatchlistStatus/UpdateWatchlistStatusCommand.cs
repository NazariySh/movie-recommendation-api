using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Watchlist;

namespace MovieRecommendation.Application.Features.Watchlist.Commands.UpdateWatchlistStatus;

public record UpdateWatchlistStatusCommand(Guid UserId, Guid MovieId, UpdateWatchlistStatusDto Model)
    : ICommand;
