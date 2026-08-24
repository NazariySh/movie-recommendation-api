using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Commands.DeleteMovie;

public class DeleteMovieCommandHandler : ICommandHandler<DeleteMovieCommand>
{
    private readonly IMovieRepository _movieRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteMovieCommandHandler(
        IMovieRepository movieRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _movieRepository = movieRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetTrackedByIdAsync(request.Id, cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with ID {request.Id} not found");
        }

        movie.IsDeleted = true;
        movie.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);
        _cache.RemoveByPrefix(MovieCacheKeys.DetailFor(movie.Id));
        _cache.RemoveByPrefix(MovieCacheKeys.SimilarFor(movie.Id));

        return Unit.Value;
    }
}
