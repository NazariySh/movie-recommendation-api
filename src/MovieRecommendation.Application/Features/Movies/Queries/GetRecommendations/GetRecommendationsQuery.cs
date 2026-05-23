using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetRecommendations;

public record GetRecommendationsQuery(Guid UserId, int Count, string Lang) : IQuery<IReadOnlyList<MovieDto>>;
