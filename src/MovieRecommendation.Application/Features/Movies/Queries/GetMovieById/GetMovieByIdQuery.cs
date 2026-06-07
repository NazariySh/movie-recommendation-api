using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetMovieById;

public record GetMovieByIdQuery(Guid Id, string Lang) : IQuery<MovieDetailDto>;
