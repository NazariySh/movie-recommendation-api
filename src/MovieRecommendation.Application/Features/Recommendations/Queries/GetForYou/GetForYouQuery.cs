using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetForYou;

public record GetForYouQuery(Guid UserId, string Lang, int Count = 20) : IQuery<IReadOnlyList<MovieDto>>;
