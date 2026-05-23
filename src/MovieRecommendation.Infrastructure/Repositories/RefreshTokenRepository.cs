using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context, IConfigurationProvider mapperConfiguration)
        : base(context, mapperConfiguration)
    {
    }

    public Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default)
    {
        return DbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, ct);
    }

    public Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return DbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAt, now), ct);
    }
}
