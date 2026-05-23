using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetArtistFilmography;

public record GetArtistFilmographyQuery(Guid Id, string? Role, string Lang)
    : IQuery<IReadOnlyList<FilmographyItemDto>>;
