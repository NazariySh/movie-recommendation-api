using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Commands.ImportMovieFromImdb;

public record ImportMovieFromImdbCommand(ImportMovieFromImdbDto Model) : ICommand<Guid>;
