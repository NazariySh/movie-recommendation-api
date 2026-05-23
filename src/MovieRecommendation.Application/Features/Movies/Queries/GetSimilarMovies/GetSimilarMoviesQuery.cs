using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetSimilarMovies;

public record GetSimilarMoviesQuery(Guid Id, int Count, string Lang) : IQuery<IReadOnlyList<MovieListItemDto>>;
