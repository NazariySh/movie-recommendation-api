using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Artists;

namespace MovieRecommendation.Application.Features.Artists.Commands.CreateArtist;

public record CreateArtistCommand(
    CreateArtistDto Model,
    ImageUpload? Photo = null) : ICommand<Guid>;
