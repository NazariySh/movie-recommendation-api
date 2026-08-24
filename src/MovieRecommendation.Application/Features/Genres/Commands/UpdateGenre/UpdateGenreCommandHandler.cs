using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Genres.Commands.UpdateGenre;

public class UpdateGenreCommandHandler : ICommandHandler<UpdateGenreCommand>
{
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateGenreCommandHandler(
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateGenreCommand request, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetTrackedByIdAsync(request.Id, cancellationToken);

        if (genre is null)
        {
            throw new NotFoundException($"Genre {request.Id} not found.");
        }

        var slug = Slugify.ToSlug(request.Model.Slug);
        if (string.IsNullOrEmpty(slug))
        {
            throw new AlreadyExistsException("Genre slug is required.");
        }

        if (slug != genre.Slug && await _genreRepository.SlugExistsAsync(slug, excludeId: request.Id, cancellationToken))
        {
            throw new AlreadyExistsException($"Genre with slug '{slug}' already exists.");
        }

        genre.Slug = slug;
        genre.Translations = request.Model.Translations.Select(t => new GenreTranslation
        {
            GenreId = genre.Id,
            LanguageCode = t.LanguageCode.ToLowerInvariant(),
            Name = t.Name,
        }).ToList();
        genre.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(GenreCacheKeys.Prefix);
        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);

        return Unit.Value;
    }
}
