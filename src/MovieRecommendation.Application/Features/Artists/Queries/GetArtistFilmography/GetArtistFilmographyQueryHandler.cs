using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Artists.Queries.GetArtistFilmography;

public class GetArtistFilmographyQueryHandler
    : IQueryHandler<GetArtistFilmographyQuery, IReadOnlyList<FilmographyItemDto>>
{
    private readonly IArtistRepository _artistRepository;

    public GetArtistFilmographyQueryHandler(IArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    public async Task<IReadOnlyList<FilmographyItemDto>> Handle(
        GetArtistFilmographyQuery request,
        CancellationToken cancellationToken)
    {
        var exists = await _artistRepository.AnyAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"Artist with ID {request.Id} not found");
        }

        return await _artistRepository.GetFilmographyAsync(request.Id, request.Role, request.Lang, cancellationToken);
    }
}
