using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class AdminDashboardRepository : IAdminDashboardRepository
{
    private readonly ApplicationDbContext _db;

    public AdminDashboardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dauSince = now.AddDays(-DashboardWindows.DauDays);
        var weekSince = now.AddDays(-DashboardWindows.WeekDays);
        var monthSince = now.AddDays(-DashboardWindows.MonthDays);
        var todayUtc = now.Date;

        var totalUsers = await _db.Users.CountAsync(u => !u.IsDeleted, ct);

        var dauUsers = await _db.Ratings
            .Where(r => r.UpdatedAt >= dauSince)
            .Select(r => r.UserId)
            .Union(_db.MovieReviews.Where(r => r.CreatedAt >= dauSince).Select(r => r.UserId))
            .Distinct()
            .CountAsync(ct);

        var mauUsers = await _db.Ratings
            .Where(r => r.UpdatedAt >= monthSince)
            .Select(r => r.UserId)
            .Union(_db.MovieReviews.Where(r => r.CreatedAt >= monthSince).Select(r => r.UserId))
            .Distinct()
            .CountAsync(ct);

        var totalMovies = await _db.Movies.CountAsync(m => !m.IsDeleted && m.Type == TitleType.Movie, ct);
        var totalSeries = await _db.Movies.CountAsync(m => !m.IsDeleted && m.Type == TitleType.Series, ct);

        var totalRatings = await _db.Ratings.CountAsync(ct);
        var ratingsToday = await _db.Ratings.CountAsync(r => r.CreatedAt >= todayUtc, ct);
        var ratingsLastWeek = await _db.Ratings.CountAsync(r => r.CreatedAt >= weekSince, ct);
        var ratingsLastMonth = await _db.Ratings.CountAsync(r => r.CreatedAt >= monthSince, ct);

        var totalReviews = await _db.MovieReviews
            .CountAsync(r => r.ParentReviewId == null && !r.IsDeleted, ct);

        var topGenres = await _db.Ratings
            .Where(r => r.CreatedAt >= monthSince)
            .SelectMany(r => r.Movie.MovieGenres)
            .GroupBy(mg => mg.Genre.Slug)
            .Select(g => new DashboardTopGenreDto { Slug = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(DashboardWindows.TopGenresLimit)
            .ToListAsync(ct);

        var model = await _db.MlModelMetadata
            .OrderByDescending(m => m.TrainedAt)
            .Select(m => new DashboardModelStatusDto
            {
                LastTrainedAt = m.TrainedAt,
                SamplesCount = m.SampleCount,
                Rmse = m.Rmse,
            })
            .FirstOrDefaultAsync(ct);

        return new DashboardSummaryDto
        {
            TotalUsers = totalUsers,
            Dau = dauUsers,
            Mau = mauUsers,
            TotalMovies = totalMovies,
            TotalSeries = totalSeries,
            TotalRatings = totalRatings,
            RatingsToday = ratingsToday,
            RatingsLast7Days = ratingsLastWeek,
            RatingsLast30Days = ratingsLastMonth,
            TotalReviews = totalReviews,
            TopGenres = topGenres,
            Model = model,
        };
    }

    public async Task<DashboardActivityDto> GetActivityAsync(int days, CancellationToken ct = default)
    {
        var clampedDays = Math.Clamp(days, DashboardWindows.ActivityDaysMin, DashboardWindows.ActivityDaysMax);
        var todayUtc = DateTime.UtcNow.Date;
        var fromUtc = todayUtc.AddDays(-(clampedDays - 1));

        var newUsers = await _db.Users
            .Where(u => u.CreatedAt >= fromUtc)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var ratings = await _db.Ratings
            .Where(r => r.CreatedAt >= fromUtc)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var reviews = await _db.MovieReviews
            .Where(r => r.CreatedAt >= fromUtc && r.ParentReviewId == null && !r.IsDeleted)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var newUsersByDay = newUsers.ToDictionary(x => x.Key, x => x.Count);
        var ratingsByDay = ratings.ToDictionary(x => x.Key, x => x.Count);
        var reviewsByDay = reviews.ToDictionary(x => x.Key, x => x.Count);

        var points = new List<DashboardActivityPointDto>(clampedDays);
        for (var i = 0; i < clampedDays; i++)
        {
            var day = fromUtc.AddDays(i);
            points.Add(new DashboardActivityPointDto
            {
                Date = DateOnly.FromDateTime(day),
                NewUsers = newUsersByDay.GetValueOrDefault(day),
                Ratings = ratingsByDay.GetValueOrDefault(day),
                Reviews = reviewsByDay.GetValueOrDefault(day),
            });
        }

        return new DashboardActivityDto
        {
            From = fromUtc,
            To = todayUtc,
            Points = points,
        };
    }
}
