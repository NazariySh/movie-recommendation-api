using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Genres.Commands.DeleteGenre;

public record DeleteGenreCommand(int Id) : ICommand;
