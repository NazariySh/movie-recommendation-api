using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IArtistRepository : IRepository<Person>
{
    Task<PagedList<ArtistDto>> GetAllPaginatedAsync(
        SearchArtistsDto searchDto,
        CancellationToken cancellationToken = default);

    Task<ArtistDetailDto?> GetDetailByIdAsync(Guid id, string lang, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FilmographyItemDto>> GetFilmographyAsync(
        Guid artistId,
        string? roleFilter,
        string lang,
        CancellationToken cancellationToken = default);

    Task<bool> HasMovieCastAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Person>> GetByImdbIdsAsync(
        IReadOnlyCollection<string> imdbIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Person>> GetBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Person>> GetByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);

    Task<Person?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ArtistSuggestionDto>> SearchSuggestionsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountSearchMatchesAsync(
        string query,
        CancellationToken cancellationToken = default);
}
