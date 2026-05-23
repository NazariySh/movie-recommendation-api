using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetArtistById;

public record GetArtistByIdQuery(Guid Id, string Lang) : IQuery<ArtistDetailDto>;
