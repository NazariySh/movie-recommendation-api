using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IMovieRepository : IRepository<Movie>
{
    Task<PagedList<MovieListItemDto>> GetAllPaginatedAsync(
        SearchMoviesDto searchDto,
        string lang,
        CancellationToken cancellationToken = default);

    Task<MovieDetailDto?> GetDetailByIdAsync(Guid id, string lang, CancellationToken cancellationToken = default);

    Task<MovieDetailDto?> GetDetailByKeyAsync(string key, string lang, CancellationToken cancellationToken = default);

    Task<MovieDetailDto?> GetDetailByImdbIdAsync(string imdbId, string lang, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        string lang,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieListItemDto>> GetListItemsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        string lang,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieListItemDto>> GetSimilarAsync(
        Guid movieId,
        int count,
        string lang,
        CancellationToken cancellationToken = default);

    Task<Movie?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Movie?> GetTrackedByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetExistingImdbIdsAsync(
        IReadOnlyCollection<string> imdbIds,
        CancellationToken cancellationToken = default);

    Task<Movie?> GetForEmbeddingAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Movie?> GetForAdminEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> KeyExistsAsync(string key, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetTrendingIdsAsync(
        TitleType type,
        int daysWindow,
        double halfLifeDays,
        double ratingCentre,
        int minRatings,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetPopularIdsAsync(
        TitleType? type,
        string? genreSlug,
        int minRatings,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieSuggestionDto>> SearchSuggestionsAsync(
        string query,
        TitleType? type,
        int limit,
        string lang,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovieSuggestionDto>> GetSuggestionsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        string lang,
        CancellationToken cancellationToken = default);

    Task<int> CountSearchMatchesAsync(
        string query,
        TitleType? type,
        string lang,
        CancellationToken cancellationToken = default);
}
