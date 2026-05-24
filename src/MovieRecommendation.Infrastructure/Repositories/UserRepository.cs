using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Models;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfigurationProvider _mapperConfiguration;

    public UserRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
    {
        _dbContext = dbContext;
        _mapperConfiguration = mapperConfiguration;
    }

    public Task<User?> GetTrackedIncludingDeletedAsync(Guid userId, CancellationToken ct = default)
    {
        return _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task<TProjection?> GetByIdAsync<TProjection>(Guid userId, CancellationToken ct = default)
        where TProjection : class
    {
        return await _dbContext.Users
            .Where(u => u.Id == userId)
            .ProjectTo<TProjection>(_mapperConfiguration)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return !await _dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);
    }

    public async Task<bool> IsUsernameUniqueAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.ToUpperInvariant();
        return !await _dbContext.Users.AnyAsync(u => u.NormalizedUserName == normalized, ct);
    }

    public async Task<PublicProfileDto?> GetPublicProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new PublicProfileDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                CreatedAt = u.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return null;
        }

        user.TotalRatings = await _dbContext.Ratings
            .CountAsync(r => r.UserId == userId, ct);

        user.TotalReviews = await _dbContext.MovieReviews
            .CountAsync(r => r.UserId == userId && !r.IsDeleted, ct);

        user.CompletedCount = await _dbContext.WatchlistItems
            .CountAsync(w => w.UserId == userId && w.Status == WatchlistStatus.Completed, ct);

        user.AverageRatingGiven = await _dbContext.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => (decimal?)r.Score)
            .AverageAsync(ct);

        return user;
    }

    public async Task<UserStatsDto?> GetStatsAsync(Guid userId, string lang, CancellationToken ct = default)
    {
        var profileExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && !u.IsDeleted, ct);

        if (!profileExists)
        {
            return null;
        }

        var stats = new UserStatsDto();

        var ratings = await _dbContext.Ratings
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new { r.Score, r.CreatedAt })
            .ToListAsync(ct);

        stats.TotalRatings = ratings.Count;
        stats.AverageRatingGiven = ratings.Count > 0 ? ratings.Average(r => r.Score) : null;

        var distribution = new int[10];
        foreach (var r in ratings)
        {
            var bucket = (int)Math.Clamp(Math.Floor((double)r.Score) - 1, 0, 9);
            distribution[bucket]++;
        }
        stats.RatingDistribution = distribution.ToList();

        var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        stats.RatingsThisMonth = ratings.Count(r => r.CreatedAt >= firstOfMonth);
        stats.LongestStreak = ComputeLongestDailyStreak(ratings.Select(r => r.CreatedAt));

        stats.TotalReviews = await _dbContext.MovieReviews
            .CountAsync(r => r.UserId == userId && !r.IsDeleted, ct);

        var watchlistGroups = await _dbContext.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var counts = new WatchlistCountsDto();
        foreach (var g in watchlistGroups)
        {
            switch (g.Status)
            {
                case WatchlistStatus.PlanToWatch: counts.PlanToWatch = g.Count; break;
                case WatchlistStatus.Watching: counts.Watching = g.Count; break;
                case WatchlistStatus.Completed: counts.Completed = g.Count; break;
                case WatchlistStatus.Dropped: counts.Dropped = g.Count; break;
            }
        }
        stats.WatchlistCounts = counts;

        stats.TotalRuntimeMinutesWatched = await _dbContext.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Status == WatchlistStatus.Completed)
            .Join(_dbContext.Movies,
                  w => w.MovieId,
                  m => m.Id,
                  (w, m) => m.Runtime ?? 0)
            .SumAsync(ct);

        var genreCounts = await _dbContext.WatchlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Status == WatchlistStatus.Completed)
            .SelectMany(w => w.Movie.MovieGenres)
            .GroupBy(mg => new { mg.GenreId, mg.Genre.Slug })
            .Select(g => new { g.Key.GenreId, g.Key.Slug, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync(ct);

        if (genreCounts.Count == 0 && ratings.Count > 0)
        {
            genreCounts = await _dbContext.Ratings
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .SelectMany(r => r.Movie.MovieGenres)
                .GroupBy(mg => new { mg.GenreId, mg.Genre.Slug })
                .Select(g => new { g.Key.GenreId, g.Key.Slug, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync(ct);
        }

        if (genreCounts.Count > 0)
        {
            var ids = genreCounts.Select(g => g.GenreId).ToList();
            var translations = await _dbContext.GenreTranslations
                .AsNoTracking()
                .Where(t => ids.Contains(t.GenreId) && (t.LanguageCode == lang || t.LanguageCode == LanguageCodes.English))
                .ToListAsync(ct);

            stats.TopGenres = genreCounts
                .Select(g =>
                {
                    var inLang = translations.FirstOrDefault(t => t.GenreId == g.GenreId && t.LanguageCode == lang);
                    var fallback = translations.FirstOrDefault(t => t.GenreId == g.GenreId && t.LanguageCode == LanguageCodes.English);
                    return new TopGenreDto
                    {
                        Slug = g.Slug,
                        Name = inLang?.Name ?? fallback?.Name ?? g.Slug,
                        Count = g.Count,
                    };
                })
                .ToList();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.CreatedAt })
            .FirstAsync(ct);

        stats.MemberSinceDays = (int)(DateTime.UtcNow - user.CreatedAt).TotalDays;

        return stats;
    }

    public async Task<PagedList<AdminUserListItemDto>> SearchAdminAsync(
        SearchAdminUsersDto query,
        CancellationToken ct = default)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = Math.Clamp(
            query.PageSize <= 0 ? AdminPagingDefaults.DefaultPageSize : query.PageSize,
            1,
            AdminPagingDefaults.MaxPageSize);
        var now = DateTimeOffset.UtcNow;

        var users = _dbContext.Users.AsNoTracking().IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            users = users.Where(u =>
                EF.Functions.ILike(u.UserName!, term) ||
                EF.Functions.ILike(u.Email!, term));
        }

        if (query.IsActive.HasValue)
        {
            users = query.IsActive.Value
                ? users.Where(u => !u.IsDeleted)
                : users.Where(u => u.IsDeleted);
        }

        if (query.IsLocked.HasValue)
        {
            users = query.IsLocked.Value
                ? users.Where(u => u.LockoutEnd != null && u.LockoutEnd > now)
                : users.Where(u => u.LockoutEnd == null || u.LockoutEnd <= now);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim();
            var userIdsInRole =
                from ur in _dbContext.Set<IdentityUserRole<Guid>>()
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                where r.Name == role
                select ur.UserId;

            users = users.Where(u => userIdsInRole.Contains(u.Id));
        }

        users = ApplyAdminUserSort(users, query.Sort, query.Order);

        var totalCount = await users.CountAsync(ct);

        var pageRows = await users
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.AvatarUrl,
                u.EmailConfirmed,
                u.IsDeleted,
                u.CreatedAt,
                u.LockoutEnd,
            })
            .ToListAsync(ct);

        var ids = pageRows.Select(u => u.Id).ToList();
        var rolesById = await (
            from ur in _dbContext.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join r in _dbContext.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name })
            .ToListAsync(ct);

        var rolesLookup = rolesById
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? string.Empty).ToList());

        var items = pageRows
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                AvatarUrl = u.AvatarUrl,
                Roles = rolesLookup.TryGetValue(u.Id, out var rs) ? rs : [],
                IsActive = !u.IsDeleted,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > now,
                EmailConfirmed = u.EmailConfirmed,
                CreatedAt = u.CreatedAt,
                LockoutEnd = u.LockoutEnd?.UtcDateTime,
            })
            .ToList();

        return new PagedList<AdminUserListItemDto>(items, pageNumber, pageSize, totalCount);
    }

    public async Task<AdminUserDetailDto?> GetAdminDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var user = await _dbContext.Users
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.AvatarUrl,
                u.Bio,
                u.PreferredLanguage,
                u.EmailConfirmed,
                u.IsDeleted,
                u.CreatedAt,
                u.LockoutEnd,
            })
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            return null;
        }

        var roles = await (
            from ur in _dbContext.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join r in _dbContext.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == userId
            select r.Name ?? string.Empty)
            .ToListAsync(ct);

        var totalRatings = await _dbContext.Ratings
            .CountAsync(r => r.UserId == userId, ct);

        var totalReviews = await _dbContext.MovieReviews
            .CountAsync(r => r.UserId == userId && r.ParentReviewId == null && !r.IsDeleted, ct);

        var totalReplies = await _dbContext.MovieReviews
            .CountAsync(r => r.UserId == userId && r.ParentReviewId != null && !r.IsDeleted, ct);

        var watchlistCount = await _dbContext.WatchlistItems
            .CountAsync(w => w.UserId == userId, ct);

        DateTime? lastRating = await _dbContext.Ratings
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => (DateTime?)r.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        DateTime? lastReview = await _dbContext.MovieReviews
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => (DateTime?)r.UpdatedAt)
            .FirstOrDefaultAsync(ct);

        DateTime? lastActivity = lastRating;
        if (lastReview.HasValue && (!lastActivity.HasValue || lastReview > lastActivity))
        {
            lastActivity = lastReview;
        }

        return new AdminUserDetailDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            PreferredLanguage = user.PreferredLanguage,
            Roles = roles,
            IsActive = !user.IsDeleted,
            IsLocked = user.LockoutEnd != null && user.LockoutEnd > now,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            LockoutEnd = user.LockoutEnd?.UtcDateTime,
            Activity = new AdminUserActivityDto
            {
                TotalRatings = totalRatings,
                TotalReviews = totalReviews,
                TotalReplies = totalReplies,
                WatchlistCount = watchlistCount,
                LastActivityAt = lastActivity,
            },
        };
    }

    private static IQueryable<User> ApplyAdminUserSort(IQueryable<User> users, string? sort, string? order)
    {
        var descending = !string.Equals(order, SortOrder.Ascending, StringComparison.OrdinalIgnoreCase);
        return (sort ?? AdminUserSortKeys.Default).ToLowerInvariant() switch
        {
            AdminUserSortKeys.Username => descending ? users.OrderByDescending(u => u.UserName) : users.OrderBy(u => u.UserName),
            AdminUserSortKeys.Email => descending ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
            _ => descending ? users.OrderByDescending(u => u.CreatedAt) : users.OrderBy(u => u.CreatedAt),
        };
    }

    private static int ComputeLongestDailyStreak(IEnumerable<DateTime> activityTimestamps)
    {
        var days = activityTimestamps
            .Select(t => t.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (days.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;
        for (var i = 1; i < days.Count; i++)
        {
            if ((days[i] - days[i - 1]).Days == 1)
            {
                current++;
                if (current > longest)
                {
                    longest = current;
                }
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }
}
