using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Domain.Entities.Movies;
using OpenAI.Embeddings;
using Pgvector;

namespace MovieRecommendation.ML.Embeddings;

public class EmbeddingGenerator : IEmbeddingService
{
    private const string EmbeddingLang = "en";

    private readonly EmbeddingClient _client;
    private readonly ILogger<EmbeddingGenerator> _logger;

    public EmbeddingGenerator(EmbeddingClient client, ILogger<EmbeddingGenerator> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Vector> GenerateMovieEmbeddingAsync(Movie movie)
    {
        var text = BuildMovieText(movie);
        return await GenerateAsync(text);
    }

    public async Task<Vector> GenerateTextEmbeddingAsync(string text)
    {
        return await GenerateAsync(text);
    }

    public async Task<IList<Vector>> GenerateBatchAsync(IEnumerable<string> texts)
    {
        var textList = texts.ToList();
        _logger.LogInformation("Generating {Count} embeddings in batch", textList.Count);

        var response = await _client.GenerateEmbeddingsAsync(textList);

        return response.Value
            .Select(e => new Vector(e.ToFloats().ToArray()))
            .ToList();
    }

    private async Task<Vector> GenerateAsync(string text)
    {
        var response = await _client.GenerateEmbeddingAsync(text);
        return new Vector(response.Value.ToFloats().ToArray());
    }

    private static string BuildMovieText(Movie movie)
    {
        var en = movie.Translations.FirstOrDefault(t => t.LanguageCode == EmbeddingLang);
        var title = en?.Title ?? movie.OriginalTitle;
        var overview = en?.Overview ?? string.Empty;

        var genres = movie.MovieGenres?
            .Select(mg => mg.Genre?.Translations.FirstOrDefault(t => t.LanguageCode == EmbeddingLang)?.Name
                ?? mg.Genre?.Slug)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            ?? [];
        var keywords = movie.Keywords?.Select(k => k.Name) ?? [];

        return $"""
            Title: {title}
            Overview: {overview}
            Genres: {string.Join(", ", genres)}
            Keywords: {string.Join(", ", keywords.Take(20))}
            Release Year: {movie.ReleaseDate?.Year}
            """;
    }
}
