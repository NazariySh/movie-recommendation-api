using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetArtistById;

public class GetArtistByIdQueryHandler : IQueryHandler<GetArtistByIdQuery, ArtistDetailDto>
{
    private readonly IArtistRepository _artistRepository;

    public GetArtistByIdQueryHandler(IArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    public async Task<ArtistDetailDto> Handle(GetArtistByIdQuery request, CancellationToken cancellationToken)
    {
        var artist = await _artistRepository.GetDetailByIdAsync(request.Id, request.Lang, cancellationToken);

        if (artist is null)
        {
            throw new NotFoundException($"Artist with ID {request.Id} not found");
        }

        return artist;
    }
}
