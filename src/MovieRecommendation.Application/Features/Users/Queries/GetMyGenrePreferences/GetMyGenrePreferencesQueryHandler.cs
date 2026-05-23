using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.Application.Features.Users.Queries.GetMyGenrePreferences;

public class GetMyGenrePreferencesQueryHandler : IQueryHandler<GetMyGenrePreferencesQuery, IReadOnlyList<GenrePreferenceDto>>
{
    private const string FallbackLang = LanguageCodes.Default;

    private readonly IUserGenrePreferenceRepository _repository;

    public GetMyGenrePreferencesQueryHandler(IUserGenrePreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<GenrePreferenceDto>> Handle(GetMyGenrePreferencesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetByUserAsync(request.UserId, cancellationToken);

        return rows
            .Select(p =>
            {
                var inLang = p.Genre.Translations.FirstOrDefault(t => t.LanguageCode == request.Lang);
                var fallback = p.Genre.Translations.FirstOrDefault(t => t.LanguageCode == FallbackLang);
                return new GenrePreferenceDto
                {
                    GenreId = p.GenreId,
                    Slug = p.Genre.Slug,
                    Name = inLang?.Name ?? fallback?.Name ?? p.Genre.Slug,
                    Weight = p.Weight,
                };
            })
            .ToList();
    }
}
