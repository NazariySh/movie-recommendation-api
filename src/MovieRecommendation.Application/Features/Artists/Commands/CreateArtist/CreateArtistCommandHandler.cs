using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Features.Artists.Commands.CreateArtist;

public class CreateArtistCommandHandler : ICommandHandler<CreateArtistCommand, Guid>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IArtistSlugGenerator _slugGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateArtistCommandHandler(
        IArtistRepository artistRepository,
        IArtistSlugGenerator slugGenerator,
        IUnitOfWork unitOfWork)
    {
        _artistRepository = artistRepository;
        _slugGenerator = slugGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateArtistCommand request, CancellationToken cancellationToken)
    {
        var slug = await _slugGenerator.GenerateUniqueAsync(
            request.Model.Name,
            excludeId: null,
            cancellationToken);

        var artist = new Person
        {
            Slug = slug,
            Name = request.Model.Name,
            PhotoUrl = request.Model.PhotoUrl,
            Birthday = request.Model.Birthday,
            DateOfDeath = request.Model.DateOfDeath,
            PlaceOfBirth = request.Model.PlaceOfBirth,
            Nationality = request.Model.Nationality,
            Gender = request.Model.Gender,
            KnownForDepartment = request.Model.KnownForDepartment,
            Biography = request.Model.Biography,
            TmdbId = request.Model.TmdbId,
            ImdbId = request.Model.ImdbId,
        };

        _artistRepository.Add(artist);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return artist.Id;
    }
}
