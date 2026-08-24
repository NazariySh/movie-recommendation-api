using System.Net;
using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Genres.Commands.DeleteGenre;

public class DeleteGenreCommandHandler : ICommandHandler<DeleteGenreCommand>
{
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteGenreCommandHandler(
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteGenreCommand request, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetTrackedByIdAsync(request.Id, cancellationToken);

        if (genre is null)
        {
            throw new NotFoundException($"Genre {request.Id} not found.");
        }

        if (await _genreRepository.HasMoviesAsync(request.Id, cancellationToken))
        {
            throw new DomainException(
                HttpStatusCode.Conflict,
                "Cannot delete a genre that is still referenced from movies. Reassign the movies first.");
        }

        _genreRepository.Remove(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(GenreCacheKeys.Prefix);

        return Unit.Value;
    }
}
