using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Genres;

namespace MovieRecommendation.Application.Features.Genres.Commands.UpdateGenre;

public record UpdateGenreCommand(int Id, UpdateGenreDto Model) : ICommand;
