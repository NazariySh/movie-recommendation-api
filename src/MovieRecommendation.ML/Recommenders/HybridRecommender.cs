using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.ML.Embeddings;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace MovieRecommendation.ML.Recommenders;

public class HybridRecommender : IRecommendationEngine
{
    private readonly ApplicationDbContext _db;
    private readonly VectorSimilarityService _vectorService;
    private readonly ColdStartRecommender _coldStart;
    private readonly RecommendationSettings _settings;

    public HybridRecommender(
        ApplicationDbContext db,
        VectorSimilarityService vectorService,
        ColdStartRecommender coldStart,
        IOptions<RecommendationSettings> settings)
    {
        _db = db;
        _vectorService = vectorService;
        _coldStart = coldStart;
        _settings = settings.Value;
    }

    public async Task<List<ScoredMovie>> GetPersonalRecommendationsAsync(
        Guid userId, int limit = 20, CancellationToken ct = default)
    {
        var ratingsCount = await _db.Ratings
            .CountAsync(r => r.UserId == userId, ct);

        if (ratingsCount < _settings.ColdStartRatingThreshold)
        {
            var genreIds = await _db.UserGenrePreferences
                .Where(g => g.UserId == userId)
                .Select(g => g.GenreId)
                .ToListAsync(ct);

            return await _coldStart
                .GetColdStartRecommendationsAsync(genreIds, limit, ct);
        }

        var tasteVector = await BuildTasteVectorAsync(userId, ct);

        var excludedMovieIds = await _db.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => r.MovieId)
            .Union(_db.WatchlistItems
                .Where(w => w.UserId == userId
                    && (w.Status == WatchlistStatus.Completed || w.Status == WatchlistStatus.Dropped))
                .Select(w => w.MovieId))
            .ToListAsync(ct);

        var excludedSet = excludedMovieIds.ToHashSet();

        var cfPredictions = await _db.MlPredictions
            .Where(p => p.UserId == userId && !excludedSet.Contains(p.MovieId))
            .OrderByDescending(p => p.PredictedScore)
            .Take(_settings.CandidatePoolSize)
            .Select(p => new { p.MovieId, p.PredictedScore })
            .ToListAsync(ct);

        if (cfPredictions.Count == 0)
        {
            var genreIds = await _db.UserGenrePreferences
                .Where(g => g.UserId == userId)
                .Select(g => g.GenreId)
                .ToListAsync(ct);
            return await _coldStart.GetColdStartRecommendationsAsync(genreIds, limit, ct);
        }

        var candidateIds = cfPredictions.Select(p => p.MovieId).ToList();

        var movieData = await _db.Movies
            .Where(m => candidateIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                m.Embedding,
                Distance = m.Embedding == null
                    ? (double?)null
                    : m.Embedding.CosineDistance(tasteVector),
            })
            .ToListAsync(ct);

        var scored = cfPredictions
            .Join(movieData,
                cf => cf.MovieId,
                md => md.Id,
                (cf, md) =>
                {
                    var cfNorm = NormalizeCfScore((double)cf.PredictedScore);
                    var semNorm = md.Distance.HasValue
                        ? NormalizeCosineDistance(md.Distance.Value)
                        : 0.5;

                    var hybrid = _settings.CfWeight * cfNorm
                        + _settings.SemanticWeight * semNorm;

                    return new ScoredCandidate(md.Id, hybrid, md.Embedding);
                })
            .OrderByDescending(x => x.Score)
            .Take(limit * 3)
            .ToList();

        return ApplyMmrReRank(scored, limit, "Recommended for you");
    }

    public async Task<List<ScoredMovie>> GetSimilarMoviesAsync(
        Guid movieId, int limit = 10, CancellationToken ct = default)
    {
        var sourceEmbedding = await _db.Movies
            .Where(m => m.Id == movieId)
            .Select(m => m.Embedding)
            .FirstOrDefaultAsync(ct);

        if (sourceEmbedding is null)
        {
            return [];
        }

        var nearest = await _db.Movies
            .Where(m => m.Id != movieId && m.Embedding != null)
            .OrderBy(m => m.Embedding!.CosineDistance(sourceEmbedding))
            .Take(limit * 3)
            .Select(m => new
            {
                m.Id,
                m.Embedding,
                Distance = m.Embedding!.CosineDistance(sourceEmbedding),
            })
            .ToListAsync(ct);

        var candidates = nearest
            .Select(n => new ScoredCandidate(
                n.Id,
                NormalizeCosineDistance(n.Distance),
                n.Embedding))
            .ToList();

        return ApplyMmrReRank(candidates, limit, "Similar movie");
    }

    public async Task<List<ScoredMovie>> GetBecauseYouWatchedAsync(
        Guid userId, Guid sourceMovieId, int limit = 10, CancellationToken ct = default)
    {
        var sourceEmbedding = await _db.Movies
            .Where(m => m.Id == sourceMovieId)
            .Select(m => m.Embedding)
            .FirstOrDefaultAsync(ct);

        if (sourceEmbedding is null)
        {
            return [];
        }

        var excludedMovieIds = await _db.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => r.MovieId)
            .Union(_db.WatchlistItems
                .Where(w => w.UserId == userId
                    && (w.Status == WatchlistStatus.Completed || w.Status == WatchlistStatus.Dropped))
                .Select(w => w.MovieId))
            .ToListAsync(ct);

        var excluded = excludedMovieIds.ToHashSet();
        excluded.Add(sourceMovieId);

        var nearest = await _db.Movies
            .Where(m => !excluded.Contains(m.Id) && m.Embedding != null)
            .OrderBy(m => m.Embedding!.CosineDistance(sourceEmbedding))
            .Take(_settings.CandidatePoolSize)
            .Select(m => new
            {
                m.Id,
                m.Embedding,
                Distance = m.Embedding!.CosineDistance(sourceEmbedding),
            })
            .ToListAsync(ct);

        var candidateIds = nearest.Select(n => n.Id).ToList();

        var cfScores = await _db.MlPredictions
            .Where(p => p.UserId == userId && candidateIds.Contains(p.MovieId))
            .ToDictionaryAsync(p => p.MovieId, p => (double)p.PredictedScore, ct);

        var candidates = nearest
            .Select(n =>
            {
                var semNorm = NormalizeCosineDistance(n.Distance);
                var cfNorm = cfScores.TryGetValue(n.Id, out var cf)
                    ? NormalizeCfScore(cf)
                    : 0.5;

                var blended = _settings.SemanticWeight * semNorm
                    + _settings.CfWeight * cfNorm;

                return new ScoredCandidate(n.Id, blended, n.Embedding);
            })
            .OrderByDescending(x => x.Score)
            .Take(limit * 3)
            .ToList();

        return ApplyMmrReRank(candidates, limit, "Because you watched");
    }

    public async Task<List<ScoredMovie>> GetColdStartRecommendationsAsync(
        IEnumerable<int> genreIds, int limit = 20, CancellationToken ct = default) =>
        await _coldStart.GetColdStartRecommendationsAsync(genreIds, limit, ct);

    private async Task<Vector> BuildTasteVectorAsync(Guid userId, CancellationToken ct)
    {
        var likedThreshold = (decimal)_settings.LikedThreshold;
        var dislikedThreshold = (decimal)_settings.DislikedThreshold;

        var liked = await _db.Ratings
            .Where(r => r.UserId == userId && r.Score >= likedThreshold)
            .Include(r => r.Movie)
            .Where(r => r.Movie.Embedding != null)
            .Select(r => r.Movie.Embedding!)
            .ToListAsync(ct);

        var disliked = await _db.Ratings
            .Where(r => r.UserId == userId && r.Score <= dislikedThreshold)
            .Include(r => r.Movie)
            .Where(r => r.Movie.Embedding != null)
            .Select(r => r.Movie.Embedding!)
            .ToListAsync(ct);

        if (liked.Count == 0)
        {
            return _vectorService.Average(disliked);
        }

        var likedCentroid = _vectorService.Average(liked);
        if (disliked.Count == 0)
        {
            return likedCentroid;
        }

        var dislikedCentroid = _vectorService.Average(disliked);
        return SubtractScaled(likedCentroid, dislikedCentroid, _settings.NegativeSignalWeight);
    }

    private static Vector SubtractScaled(Vector positive, Vector negative, double weight)
    {
        var p = positive.ToArray();
        var n = negative.ToArray();
        var result = new float[p.Length];
        for (int i = 0; i < p.Length; i++)
        {
            result[i] = p[i] - (float)(weight * n[i]);
        }
        return new Vector(result);
    }

    private static double NormalizeCfScore(double score) =>
        Math.Clamp((score - 1.0) / 9.0, 0.0, 1.0);

    private static double NormalizeCosineDistance(double distance) =>
        Math.Clamp(1.0 - distance / 2.0, 0.0, 1.0);

    private List<ScoredMovie> ApplyMmrReRank(
        List<ScoredCandidate> candidates, int limit, string reason)
    {
        if (candidates.Count <= limit)
        {
            return candidates
                .Select(c => new ScoredMovie(c.MovieId, c.Score, reason))
                .ToList();
        }

        var lambda = _settings.MmrLambda;
        var picked = new List<ScoredCandidate>(limit);
        var pool = new List<ScoredCandidate>(candidates);

        picked.Add(pool[0]);
        pool.RemoveAt(0);

        while (picked.Count < limit && pool.Count > 0)
        {
            var bestIdx = 0;
            var bestScore = double.NegativeInfinity;

            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                var maxSim = 0.0;

                if (c.Embedding is not null)
                {
                    foreach (var p in picked)
                    {
                        if (p.Embedding is null) continue;
                        var sim = _vectorService.CosineSimilarity(c.Embedding, p.Embedding);
                        if (sim > maxSim) maxSim = sim;
                    }
                }

                var mmr = lambda * c.Score - (1 - lambda) * maxSim;
                if (mmr > bestScore)
                {
                    bestScore = mmr;
                    bestIdx = i;
                }
            }

            picked.Add(pool[bestIdx]);
            pool.RemoveAt(bestIdx);
        }

        return picked
            .Select(c => new ScoredMovie(c.MovieId, c.Score, reason))
            .ToList();
    }

    private sealed record ScoredCandidate(Guid MovieId, double Score, Vector? Embedding);
}
