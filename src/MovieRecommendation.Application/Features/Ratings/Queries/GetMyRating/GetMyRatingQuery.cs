using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Ratings;

namespace MovieRecommendation.Application.Features.Ratings.Queries.GetMyRating;

public record GetMyRatingQuery(Guid UserId, Guid MovieId) : IQuery<RatingDto?>;
