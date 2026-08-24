using Pgvector;

namespace MovieRecommendation.ML.Embeddings;

public class VectorSimilarityService
{
    public double CosineDistance(Vector a, Vector b)
    {
        var fa = a.ToArray();
        var fb = b.ToArray();

        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < fa.Length; i++)
        {
            dot += fa[i] * fb[i];
            normA += fa[i] * fa[i];
            normB += fb[i] * fb[i];
        }

        if (normA == 0 || normB == 0)
            return 1.0;
        return 1.0 - dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    public double CosineSimilarity(Vector a, Vector b) =>
        1.0 - CosineDistance(a, b);

    public Vector Average(IList<Vector> vectors)
    {
        if (!vectors.Any())
            return new Vector(new float[1536]);

        var size = vectors[0].ToArray().Length;
        var result = new float[size];

        foreach (var vec in vectors)
        {
            var arr = vec.ToArray();
            for (int i = 0; i < size; i++)
                result[i] += arr[i];
        }

        for (int i = 0; i < size; i++)
            result[i] /= vectors.Count;

        return new Vector(result);
    }
}
