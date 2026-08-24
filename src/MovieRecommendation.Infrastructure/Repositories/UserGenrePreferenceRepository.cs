using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class UserGenrePreferenceRepository : BaseRepository<UserGenrePreference>, IUserGenrePreferenceRepository
{
    public UserGenrePreferenceRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public async Task<IReadOnlyList<int>> GetGenreIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UserGenrePreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.Weight)
            .Select(p => p.GenreId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserGenrePreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UserGenrePreferences
            .AsNoTracking()
            .Include(p => p.Genre)
            .ThenInclude(g => g.Translations)
            .Where(p => p.UserId == userId && !p.IsDeleted)
            .OrderByDescending(p => p.Weight)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForUserAsync(
        Guid userId,
        IReadOnlyCollection<UserGenrePreference> preferences,
        CancellationToken cancellationToken = default)
    {
        var existing = await DbContext.UserGenrePreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            DbContext.UserGenrePreferences.RemoveRange(existing);
        }

        if (preferences.Count > 0)
        {
            await DbContext.UserGenrePreferences.AddRangeAsync(preferences, cancellationToken);
        }
    }
}
