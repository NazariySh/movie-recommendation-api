using MovieRecommendation.Domain.Entities.Movies;
using Pgvector;

namespace MovieRecommendation.Application.Interfaces.ML;

public interface IEmbeddingService
{
    Task<Vector> GenerateMovieEmbeddingAsync(Movie movie);

    Task<Vector> GenerateTextEmbeddingAsync(string text);

    Task<IList<Vector>> GenerateBatchAsync(IEnumerable<string> texts);
}
