using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Infrastructure.Data;
using Pgvector.EntityFrameworkCore;

namespace MovieRecommendation.ML.Recommenders;

public class SemanticSearchEngine : ISearchEngine
{
    private readonly ApplicationDbContext _db;
    private readonly IEmbeddingService _embeddingService;

    public SemanticSearchEngine(
        ApplicationDbContext db,
        IEmbeddingService embeddingService)
    {
        _db = db;
        _embeddingService = embeddingService;
    }

    public async Task<List<ScoredMovie>> SemanticSearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var queryVector = await _embeddingService
            .GenerateTextEmbeddingAsync(query);

        var results = await _db.Movies
            .Where(m => m.Embedding != null)
            .Select(m => new { m.Id, Distance = m.Embedding!.CosineDistance(queryVector) })
            .OrderBy(r => r.Distance)
            .Take(limit)
            .ToListAsync(ct);

        return results.Select(r => new ScoredMovie(
            MovieId: r.Id,
            Score: Math.Clamp(1.0 - r.Distance / 2.0, 0.0, 1.0),
            Reason: "Semantic match"))
            .ToList();
    }
}
