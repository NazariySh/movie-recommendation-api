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
    private readonly MmrReRanker _mmr;
    private readonly RecommendationSettings _settings;

    public HybridRecommender(
        ApplicationDbContext db,
        VectorSimilarityService vectorService,
        ColdStartRecommender coldStart,
        MmrReRanker mmr,
        IOptions<RecommendationSettings> settings)
    {
        _db = db;
        _vectorService = vectorService;
        _coldStart = coldStart;
        _mmr = mmr;
        _settings = settings.Value;
    }

    public async Task<List<ScoredMovie>> GetPersonalRecommendationsAsync(
        Guid userId, int limit = 20, CancellationToken ct = default)
    {
        var ratingsCount = await _db.Ratings
            .CountAsync(r => r.UserId == userId, ct);

        if (ratingsCount < _settings.ColdStartRatingThreshold)
        {
            return await ColdStartFallbackAsync(userId, limit, ct);
        }

        var builtTaste = await BuildTasteVectorAsync(userId, ct);
        if (builtTaste is null)
        {
            return await ColdStartFallbackAsync(userId, limit, ct);
        }

        Vector tasteVector = builtTaste;

        var excludedSet = await GetUserExclusionsAsync(userId, ct);

        var cfPredictions = await _db.MlPredictions
            .Where(p => p.UserId == userId && !excludedSet.Contains(p.MovieId))
            .OrderByDescending(p => p.PredictedScore)
            .Take(_settings.CandidatePoolSize)
            .Select(p => new { p.MovieId, p.PredictedScore })
            .ToListAsync(ct);

        if (cfPredictions.Count == 0)
        {
            return await ColdStartFallbackAsync(userId, limit, ct);
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

        return _mmr.ReRank(scored, limit, _settings.MmrLambda, RecommendationReason.ForYou);
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

        return _mmr.ReRank(candidates, limit, _settings.MmrLambda, RecommendationReason.Similar);
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

        var excluded = await GetUserExclusionsAsync(userId, ct);
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

        return _mmr.ReRank(candidates, limit, _settings.MmrLambda, RecommendationReason.BecauseWatched);
    }

    public async Task<List<ScoredMovie>> GetColdStartRecommendationsAsync(
        IEnumerable<int> genreIds, int limit = 20, CancellationToken ct = default) =>
        await _coldStart.GetColdStartRecommendationsAsync(genreIds, limit, ct);

    private async Task<List<ScoredMovie>> ColdStartFallbackAsync(Guid userId, int limit, CancellationToken ct)
    {
        var genreIds = await _db.UserGenrePreferences
            .Where(g => g.UserId == userId)
            .Select(g => g.GenreId)
            .ToListAsync(ct);

        var excluded = await GetUserExclusionsAsync(userId, ct);

        return await _coldStart.GetColdStartRecommendationsAsync(genreIds, limit, ct, excluded);
    }

    private async Task<HashSet<Guid>> GetUserExclusionsAsync(Guid userId, CancellationToken ct)
    {
        var excludedMovieIds = await _db.Ratings
            .Where(r => r.UserId == userId)
            .Select(r => r.MovieId)
            .Union(_db.WatchlistItems
                .Where(w => w.UserId == userId
                    && (w.Status == WatchlistStatus.Completed || w.Status == WatchlistStatus.Dropped))
                .Select(w => w.MovieId))
            .ToListAsync(ct);

        return excludedMovieIds.ToHashSet();
    }

    private async Task<Vector?> BuildTasteVectorAsync(Guid userId, CancellationToken ct)
    {
        var likedThreshold = (decimal)_settings.LikedThreshold;
        var dislikedThreshold = (decimal)_settings.DislikedThreshold;

        var bucketed = await _db.Ratings
            .Where(r => r.UserId == userId
                && (r.Score >= likedThreshold || r.Score <= dislikedThreshold)
                && r.Movie.Embedding != null)
            .Select(r => new
            {
                IsLiked = r.Score >= likedThreshold,
                Embedding = r.Movie.Embedding!,
            })
            .ToListAsync(ct);

        var liked = bucketed.Where(x => x.IsLiked).Select(x => x.Embedding).ToList();
        var disliked = bucketed.Where(x => !x.IsLiked).Select(x => x.Embedding).ToList();

        if (liked.Count == 0 && disliked.Count == 0)
        {
            return null;
        }

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
}
