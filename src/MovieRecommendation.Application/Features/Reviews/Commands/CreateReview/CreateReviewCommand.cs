using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Reviews;

namespace MovieRecommendation.Application.Features.Reviews.Commands.CreateReview;

public record CreateReviewCommand(Guid UserId, Guid MovieId, CreateMovieReviewDto Model)
    : ICommand<MovieReviewDto>;
