using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetAllArtists;

public record GetAllArtistsQuery(SearchArtistsDto Model) : IQuery<PagedList<ArtistDto>>;
