using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Reviews;

namespace MovieRecommendation.Application.Features.Reviews.Queries.GetReviewReplies;

public record GetReviewRepliesQuery(Guid ReviewId, Guid ViewerId)
    : IQuery<IReadOnlyList<MovieReviewDto>>;
