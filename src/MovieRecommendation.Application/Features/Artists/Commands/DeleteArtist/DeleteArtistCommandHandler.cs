using System.Net;
using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Artists.Commands.DeleteArtist;

public class DeleteArtistCommandHandler : ICommandHandler<DeleteArtistCommand>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteArtistCommandHandler(IArtistRepository artistRepository, IUnitOfWork unitOfWork)
    {
        _artistRepository = artistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteArtistCommand request, CancellationToken cancellationToken)
    {
        var artist = await _artistRepository.GetByIdTrackedAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Artist with ID {request.Id} not found");

        if (await _artistRepository.HasMovieCastAsync(request.Id, cancellationToken))
        {
            throw new DomainException(
                HttpStatusCode.Conflict,
                "Cannot delete artist while they are referenced from movie casts. Remove the cast entries first.");
        }

        artist.IsDeleted = true;
        artist.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
