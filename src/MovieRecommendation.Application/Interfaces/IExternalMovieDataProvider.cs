using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Interfaces;

public interface IExternalMovieDataProvider
{
    Task<ExternalMovieResult?> FetchByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);

    Task<ExternalTranslationResult?> FetchTranslationAsync(
        int tmdbId,
        TitleType type,
        string languageCode,
        CancellationToken cancellationToken = default);
}
