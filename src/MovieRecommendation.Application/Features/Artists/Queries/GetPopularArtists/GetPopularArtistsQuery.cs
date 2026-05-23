using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetPopularArtists;

public record GetPopularArtistsQuery(int Count = 12) : IQuery<IReadOnlyList<ArtistDto>>;
