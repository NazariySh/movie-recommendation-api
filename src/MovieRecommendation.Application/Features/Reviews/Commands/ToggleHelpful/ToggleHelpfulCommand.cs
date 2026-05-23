using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Reviews.Commands.ToggleHelpful;

public record ToggleHelpfulCommand(Guid ReviewId, Guid UserId) : ICommand<ToggleHelpfulResult>;
