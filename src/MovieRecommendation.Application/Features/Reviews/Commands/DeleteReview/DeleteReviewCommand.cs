using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Reviews.Commands.DeleteReview;

public record DeleteReviewCommand(Guid Id, Guid UserId, bool IsModerator) : ICommand;
