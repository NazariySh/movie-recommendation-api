using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Reviews;

namespace MovieRecommendation.Application.Features.Reviews.Commands.CreateReply;

public record CreateReplyCommand(Guid ParentReviewId, Guid UserId, CreateReplyDto Model)
    : ICommand<MovieReviewDto>;
