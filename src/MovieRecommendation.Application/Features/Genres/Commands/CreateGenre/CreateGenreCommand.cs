using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Genres;

namespace MovieRecommendation.Application.Features.Genres.Commands.CreateGenre;

public record CreateGenreCommand(CreateGenreDto Model) : ICommand<int>;
