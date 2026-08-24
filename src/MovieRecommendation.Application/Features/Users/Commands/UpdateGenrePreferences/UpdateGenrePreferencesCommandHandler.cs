using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Commands.UpdateGenrePreferences;

public class UpdateGenrePreferencesCommandHandler
    : ICommandHandler<UpdateGenrePreferencesCommand, IReadOnlyList<GenrePreferenceDto>>
{
    private const string FallbackLang = LanguageCodes.Default;

    private readonly IUserGenrePreferenceRepository _repository;
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateGenrePreferencesCommandHandler(
        IUserGenrePreferenceRepository repository,
        IGenreRepository genreRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IReadOnlyList<GenrePreferenceDto>> Handle(UpdateGenrePreferencesCommand request, CancellationToken cancellationToken)
    {
        var requestedIds = request.Preferences.Select(p => p.GenreId).Distinct().ToList();
        var genres = await _genreRepository.GetByIdsAsync(requestedIds, cancellationToken);

        var unknown = requestedIds.Where(id => !genres.ContainsKey(id)).ToList();
        if (unknown.Count > 0)
        {
            throw new NotFoundException($"Unknown genre id(s): {string.Join(", ", unknown)}");
        }

        var now = DateTime.UtcNow;
        var rows = request.Preferences
            .Select(p => new UserGenrePreference
            {
                UserId = request.UserId,
                GenreId = p.GenreId,
                Weight = Math.Clamp(p.Weight, 0m, 1m),
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        await _repository.ReplaceForUserAsync(request.UserId, rows, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(request.UserId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(request.UserId));

        return rows
            .Select(p =>
            {
                var genre = genres[p.GenreId];
                var inLang = genre.Translations.FirstOrDefault(t => t.LanguageCode == request.Lang);
                var fallback = genre.Translations.FirstOrDefault(t => t.LanguageCode == FallbackLang);
                return new GenrePreferenceDto
                {
                    GenreId = genre.Id,
                    Slug = genre.Slug,
                    Name = inLang?.Name ?? fallback?.Name ?? genre.Slug,
                    Weight = p.Weight,
                };
            })
            .ToList();
    }
}
