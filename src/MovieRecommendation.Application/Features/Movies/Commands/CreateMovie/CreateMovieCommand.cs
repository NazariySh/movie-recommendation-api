using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;

public record CreateMovieCommand(
    CreateMovieDto Model,
    ImageUpload? Poster = null,
    ImageUpload? Backdrop = null) : ICommand<Guid>;
