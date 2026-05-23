using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetAllArtists;

public class GetAllArtistsQueryHandler : IQueryHandler<GetAllArtistsQuery, PagedList<ArtistDto>>
{
    private readonly IArtistRepository _artistRepository;

    public GetAllArtistsQueryHandler(IArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    public Task<PagedList<ArtistDto>> Handle(GetAllArtistsQuery request, CancellationToken cancellationToken)
    {
        return _artistRepository.GetAllPaginatedAsync(request.Model, cancellationToken);
    }
}
