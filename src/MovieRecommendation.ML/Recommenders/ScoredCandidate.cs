using Pgvector;

namespace MovieRecommendation.ML.Recommenders;

public sealed record ScoredCandidate(Guid MovieId, double Score, Vector? Embedding);
