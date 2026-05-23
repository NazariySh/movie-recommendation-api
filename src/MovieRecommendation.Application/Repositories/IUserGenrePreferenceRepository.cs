using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Repositories;

public interface IUserGenrePreferenceRepository
{
    Task<IReadOnlyList<int>> GetGenreIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserGenrePreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ReplaceForUserAsync(
        Guid userId,
        IReadOnlyCollection<UserGenrePreference> preferences,
        CancellationToken cancellationToken = default);
}
