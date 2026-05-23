using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;

public record CreateMovieCommand(CreateMovieDto Model) : ICommand<Guid>;
