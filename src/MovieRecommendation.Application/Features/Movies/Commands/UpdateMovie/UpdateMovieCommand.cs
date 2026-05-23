using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;

public record UpdateMovieCommand(Guid Id, UpdateMovieDto Model) : ICommand;
