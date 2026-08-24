using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Genres.Commands.CreateGenre;

public class CreateGenreCommandHandler : ICommandHandler<CreateGenreCommand, int>
{
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateGenreCommandHandler(
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<int> Handle(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Model.Slug)
            ? Slugify.ToSlug(request.Model.Translations.FirstOrDefault()?.Name ?? string.Empty)
            : Slugify.ToSlug(request.Model.Slug);

        if (string.IsNullOrEmpty(slug))
        {
            throw new AlreadyExistsException("Genre slug is required.");
        }

        if (await _genreRepository.SlugExistsAsync(slug, excludeId: null, cancellationToken))
        {
            throw new AlreadyExistsException($"Genre with slug '{slug}' already exists.");
        }

        var genre = new Genre
        {
            Slug = slug,
            Translations = request.Model.Translations.Select(t => new GenreTranslation
            {
                LanguageCode = t.LanguageCode.ToLowerInvariant(),
                Name = t.Name,
            }).ToList(),
        };

        _genreRepository.Add(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(GenreCacheKeys.Prefix);

        return genre.Id;
    }
}
