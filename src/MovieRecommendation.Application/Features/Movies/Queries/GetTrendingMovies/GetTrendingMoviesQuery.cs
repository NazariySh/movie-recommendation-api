using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetTrendingMovies;

public record GetTrendingMoviesQuery(
    string Lang,
    TitleType Type = TitleType.Movie,
    int Count = 20,
    int DaysWindow = 30
) : IQuery<IReadOnlyList<MovieDto>>;
