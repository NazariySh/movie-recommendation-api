using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.ML.Embeddings;

namespace MovieRecommendation.ML.Recommenders;

public class MmrReRanker
{
    private readonly VectorSimilarityService _vectorService;

    public MmrReRanker(VectorSimilarityService vectorService)
    {
        _vectorService = vectorService;
    }

    public List<ScoredMovie> ReRank(
        IReadOnlyList<ScoredCandidate> candidates,
        int limit,
        double lambda,
        RecommendationReason reason)
    {
        if (candidates.Count <= limit)
        {
            return candidates
                .Select(c => new ScoredMovie(c.MovieId, c.Score, reason))
                .ToList();
        }

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
}
