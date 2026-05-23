using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Artists.Commands.UpdateArtist;

public class UpdateArtistCommandHandler : ICommandHandler<UpdateArtistCommand>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IArtistSlugGenerator _slugGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateArtistCommandHandler(
        IArtistRepository artistRepository,
        IArtistSlugGenerator slugGenerator,
        IUnitOfWork unitOfWork)
    {
        _artistRepository = artistRepository;
        _slugGenerator = slugGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateArtistCommand request, CancellationToken cancellationToken)
    {
        var artist = await _artistRepository.GetByIdTrackedAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Artist with ID {request.Id} not found");

        if (!string.Equals(artist.Name, request.Model.Name, StringComparison.Ordinal))
        {
            artist.Slug = await _slugGenerator.GenerateUniqueAsync(
                request.Model.Name,
                excludeId: request.Id,
                cancellationToken);
        }

        artist.Name = request.Model.Name;
        artist.PhotoUrl = request.Model.PhotoUrl;
        artist.Birthday = request.Model.Birthday;
        artist.DateOfDeath = request.Model.DateOfDeath;
        artist.PlaceOfBirth = request.Model.PlaceOfBirth;
        artist.Nationality = request.Model.Nationality;
        artist.Gender = request.Model.Gender;
        artist.KnownForDepartment = request.Model.KnownForDepartment;
        artist.Biography = request.Model.Biography;
        artist.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
