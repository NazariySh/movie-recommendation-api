using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Ratings.Commands.DeleteRating;

public record DeleteRatingCommand(Guid UserId, Guid MovieId) : ICommand;
