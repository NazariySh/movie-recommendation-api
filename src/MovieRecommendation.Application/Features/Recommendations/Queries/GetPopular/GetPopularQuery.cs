using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetPopular;

public record GetPopularQuery(
    string Lang,
    TitleType? Type = null,
    string? GenreSlug = null,
    int Count = 20
) : IQuery<IReadOnlyList<MovieDto>>;
