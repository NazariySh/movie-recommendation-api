using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetMovieByKey;

public record GetMovieByKeyQuery(string MovieKey, string Lang) : IQuery<MovieDetailDto>;
