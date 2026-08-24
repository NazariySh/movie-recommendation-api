using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetMovieByImdbId;

public record GetMovieByImdbIdQuery(string ImdbId, string Lang) : IQuery<MovieDetailDto>;
