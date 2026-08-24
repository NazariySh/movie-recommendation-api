using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Movies.Commands.DeleteMovie;

public record DeleteMovieCommand(Guid Id) : ICommand;
