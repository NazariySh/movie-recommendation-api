using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;

namespace MovieRecommendation.Application.Features.Artists.Commands.UpdateArtist;

public record UpdateArtistCommand(Guid Id, UpdateArtistDto Model) : ICommand;
