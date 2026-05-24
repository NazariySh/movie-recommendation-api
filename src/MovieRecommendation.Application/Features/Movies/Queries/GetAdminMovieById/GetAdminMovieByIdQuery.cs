using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetAdminMovieById;

public record GetAdminMovieByIdQuery(Guid Id) : IQuery<AdminMovieDetailDto>;
