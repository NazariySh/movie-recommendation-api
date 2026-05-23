using MovieRecommendation.Application.DTOs.Genres;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Repositories;

public interface IGenreRepository : IRepository<Genre, int>
{
    Task<IReadOnlyList<GenreDto>> GetAllAsync(string lang, CancellationToken cancellationToken = default);

    Task<Genre?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> HasMoviesAsync(int genreId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Genre>> GetBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, Genre>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);
}
