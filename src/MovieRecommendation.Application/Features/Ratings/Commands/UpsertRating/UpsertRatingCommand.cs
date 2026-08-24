using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Ratings;

namespace MovieRecommendation.Application.Features.Ratings.Commands.UpsertRating;

public record UpsertRatingCommand(Guid UserId, Guid MovieId, UpsertRatingDto Model) : ICommand<RatingDto>;
