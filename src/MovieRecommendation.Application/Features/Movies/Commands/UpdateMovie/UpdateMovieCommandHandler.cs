using AutoMapper;
using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;

public class UpdateMovieCommandHandler : ICommandHandler<UpdateMovieCommand>
{
    private readonly IMovieRepository _movieRepository;
    private readonly IMovieKeyGenerator _keyGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IImageMirrorService _imageMirror;
    private readonly ICacheService _cache;

    public UpdateMovieCommandHandler(
        IMovieRepository movieRepository,
        IMovieKeyGenerator keyGenerator,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IImageMirrorService imageMirror,
        ICacheService cache)
    {
        _movieRepository = movieRepository;
        _keyGenerator = keyGenerator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _imageMirror = imageMirror;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetTrackedByIdAsync(request.Id, cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with ID {request.Id} not found");
        }

        var dto = request.Model;
        var key = await _keyGenerator.GenerateUniqueAsync(
            dto.Key,
            dto.OriginalTitle,
            excludeId: request.Id,
            cancellationToken);

        _mapper.Map(dto, movie);
        movie.Key = key;
        movie.ReleaseDate = NormalizeToUtc(movie.ReleaseDate);
        movie.PosterUrl = request.Poster is not null
            ? await _imageMirror.StoreUploadAsync(request.Poster.Content, request.Poster.ContentType, $"posters/{key}", cancellationToken)
            : await _imageMirror.MirrorAsync(movie.PosterUrl, $"posters/{key}", cancellationToken);
        movie.BackdropUrl = request.Backdrop is not null
            ? await _imageMirror.StoreUploadAsync(request.Backdrop.Content, request.Backdrop.ContentType, $"backdrops/{key}", cancellationToken)
            : await _imageMirror.MirrorAsync(movie.BackdropUrl, $"backdrops/{key}", cancellationToken);

        movie.MovieGenres = dto.GenreIds.Distinct().Select(id => new MovieGenre { GenreId = id }).ToList();
        movie.Translations = dto.Translations.Select(t => new MovieTranslation
        {
            LanguageCode = t.LanguageCode.ToLowerInvariant(),
            Title = t.Title,
            Overview = t.Overview,
            Tagline = t.Tagline,
        }).ToList();

        movie.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        InvalidateMovieCaches(movie.Id);

        return Unit.Value;
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (!value.HasValue) return null;
        return value.Value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    private void InvalidateMovieCaches(Guid movieId)
    {
        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);
        _cache.RemoveByPrefix(MovieCacheKeys.DetailFor(movieId));
        _cache.RemoveByPrefix(MovieCacheKeys.SimilarFor(movieId));
    }
}
