using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Reviews;

namespace MovieRecommendation.Application.Features.Reviews.Commands.UpdateReview;

public record UpdateReviewCommand(Guid Id, Guid UserId, UpdateMovieReviewDto Model) : ICommand;
