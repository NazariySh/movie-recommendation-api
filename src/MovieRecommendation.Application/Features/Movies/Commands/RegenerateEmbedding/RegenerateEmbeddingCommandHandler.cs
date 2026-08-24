using System.Net;
using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Commands.RegenerateEmbedding;

public class RegenerateEmbeddingCommandHandler : ICommandHandler<RegenerateEmbeddingCommand>
{
    private readonly IMovieRepository _movieRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingService? _embeddingService;

    public RegenerateEmbeddingCommandHandler(
        IMovieRepository movieRepository,
        IUnitOfWork unitOfWork,
        IEmbeddingService? embeddingService = null)
    {
        _movieRepository = movieRepository;
        _unitOfWork = unitOfWork;
        _embeddingService = embeddingService;
    }

    public async Task<Unit> Handle(RegenerateEmbeddingCommand request, CancellationToken cancellationToken)
    {
        if (_embeddingService is null)
        {
            throw new DomainException(
                HttpStatusCode.ServiceUnavailable,
                "Embedding service is not configured.");
        }

        var movie = await _movieRepository.GetForEmbeddingAsync(request.Id, cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with ID {request.Id} not found");
        }

        movie.Embedding = await _embeddingService.GenerateMovieEmbeddingAsync(movie);
        movie.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
