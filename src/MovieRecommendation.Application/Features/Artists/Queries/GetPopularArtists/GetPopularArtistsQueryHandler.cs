using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetPopularArtists;

public class GetPopularArtistsQueryHandler : IQueryHandler<GetPopularArtistsQuery, IReadOnlyList<ArtistDto>>
{
    private readonly IArtistRepository _artistRepository;

    public GetPopularArtistsQueryHandler(IArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    public Task<IReadOnlyList<ArtistDto>> Handle(GetPopularArtistsQuery request, CancellationToken cancellationToken)
    {
        return _artistRepository.GetPopularAsync(request.Count, cancellationToken);
    }
}
