using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.WatchHistory;

namespace MovieRecommendation.Application.Features.WatchHistory.Commands.AddWatchHistory;

public record AddWatchHistoryCommand(Guid UserId, CreateWatchHistoryDto Model) : ICommand;
