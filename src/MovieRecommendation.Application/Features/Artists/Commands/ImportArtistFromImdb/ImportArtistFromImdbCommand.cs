using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;

namespace MovieRecommendation.Application.Features.Artists.Commands.ImportArtistFromImdb;

public record ImportArtistFromImdbCommand(ImportArtistFromImdbDto Model) : ICommand<Guid>;
