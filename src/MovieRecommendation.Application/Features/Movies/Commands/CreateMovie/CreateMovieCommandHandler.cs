using AutoMapper;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;

public class CreateMovieCommandHandler : ICommandHandler<CreateMovieCommand, Guid>
{
    private readonly IMovieRepository _movieRepository;
    private readonly IMovieKeyGenerator _keyGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmbeddingService? _embeddingService;
    private readonly ICacheService _cache;
    private readonly ILogger<CreateMovieCommandHandler> _logger;

    public CreateMovieCommandHandler(
        IMovieRepository movieRepository,
        IMovieKeyGenerator keyGenerator,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cache,
        ILogger<CreateMovieCommandHandler> logger,
        IEmbeddingService? embeddingService = null)
    {
        _movieRepository = movieRepository;
        _keyGenerator = keyGenerator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _embeddingService = embeddingService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateMovieCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Model;

        var key = await _keyGenerator.GenerateUniqueAsync(
            dto.Key,
            dto.OriginalTitle,
            excludeId: null,
            cancellationToken);

        var movie = _mapper.Map<Movie>(dto);
        movie.Key = key;
        movie.ReleaseDate = NormalizeToUtc(movie.ReleaseDate);
        movie.MovieGenres = dto.GenreIds.Distinct().Select(id => new MovieGenre { GenreId = id }).ToList();
        movie.Translations = dto.Translations.Select(t => new MovieTranslation
        {
            LanguageCode = t.LanguageCode.ToLowerInvariant(),
            Title = t.Title,
            Overview = t.Overview,
            Tagline = t.Tagline,
        }).ToList();

        _movieRepository.Add(movie);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TryComputeEmbeddingAsync(movie);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);

        return movie.Id;
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (!value.HasValue) return null;
        return value.Value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    private async Task TryComputeEmbeddingAsync(Movie movie)
    {
        if (_embeddingService is null)
        {
            return;
        }

        try
        {
            movie.Embedding = await _embeddingService.GenerateMovieEmbeddingAsync(movie);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding generation failed for movie {MovieId}; will retry later", movie.Id);
        }
    }
}
