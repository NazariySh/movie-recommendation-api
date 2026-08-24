using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Movies;

namespace MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;

public record UpdateMovieCommand(
    Guid Id,
    UpdateMovieDto Model,
    ImageUpload? Poster = null,
    ImageUpload? Backdrop = null) : ICommand;
