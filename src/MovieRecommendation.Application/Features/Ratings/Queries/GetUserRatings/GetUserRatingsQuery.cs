using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Ratings.Queries.GetUserRatings;

public record GetUserRatingsQuery(Guid UserId, int PageNumber, int PageSize, string Lang)
    : IQuery<PagedList<UserRatingDto>>;
