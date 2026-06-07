using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.ML.Recommenders;

public class ColdStartRecommender
{
    private readonly ApplicationDbContext _db;
    private readonly RecommendationSettings _settings;

    public ColdStartRecommender(
        ApplicationDbContext db,
        IOptions<RecommendationSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
    }

    public async Task<List<ScoredMovie>> GetColdStartRecommendationsAsync(
        IEnumerable<int> genreIds,
        int limit = 20,
        CancellationToken ct = default,
        IReadOnlyCollection<Guid>? excludedMovieIds = null)
    {
        var ids = genreIds.ToList();
        var hasGenreFilter = ids.Count > 0;
        var minVotes = (decimal)_settings.BayesianMinVotes;
        var globalMean = (decimal)_settings.BayesianGlobalMean;

        var movies = _db.Movies
            .Where(m => !hasGenreFilter || m.MovieGenres.Any(g => ids.Contains(g.GenreId)));

        if (excludedMovieIds is { Count: > 0 } excluded)
        {
            movies = movies.Where(m => !excluded.Contains(m.Id));
        }

        var query = movies
            .Select(m => new
            {
                m.Id,
                Bayesian = (m.RatingsCount * m.AverageRating + minVotes * globalMean)
                    / (m.RatingsCount + minVotes),
            })
            .OrderByDescending(x => x.Bayesian)
            .Take(limit);

        var results = await query.ToListAsync(ct);

        var reason = hasGenreFilter ? RecommendationReason.PopularInGenres : RecommendationReason.TopRated;

        return results
            .Select(m => new ScoredMovie(
                MovieId: m.Id,
                Score: (double)m.Bayesian / 10.0,
                Reason: reason))
            .ToList();
    }
}
