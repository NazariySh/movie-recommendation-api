using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Artists.Commands.DeleteArtist;

public record DeleteArtistCommand(Guid Id) : ICommand;
