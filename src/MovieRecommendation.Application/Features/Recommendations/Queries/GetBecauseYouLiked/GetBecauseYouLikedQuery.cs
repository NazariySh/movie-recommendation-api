using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetBecauseYouLiked;

public record GetBecauseYouLikedQuery(Guid MovieId, string Lang, Guid CurrentUserId, int Count = 12) : IQuery<IReadOnlyList<MovieDto>>;
