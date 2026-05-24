using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetColdStart;

public record GetColdStartQuery(Guid UserId, string Lang, int Count = 20) : IQuery<IReadOnlyList<MovieDto>>;
