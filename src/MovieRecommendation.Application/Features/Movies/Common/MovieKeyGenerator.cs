using System.Net;
using MovieRecommendation.Application.Common;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Common;

public interface IMovieKeyGenerator
{
    Task<string> GenerateUniqueAsync(
        string? requestedKey,
        string fallbackSource,
        Guid? excludeId,
        CancellationToken cancellationToken);
}

public class MovieKeyGenerator : IMovieKeyGenerator
{
    private const int MaxBaseKeyLength = 250;

    private readonly IMovieRepository _movieRepository;

    public MovieKeyGenerator(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<string> GenerateUniqueAsync(
        string? requestedKey,
        string fallbackSource,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var baseKey = string.IsNullOrWhiteSpace(requestedKey)
            ? Slugify.ToSlug(fallbackSource)
            : Slugify.ToSlug(requestedKey);

        if (string.IsNullOrEmpty(baseKey))
        {
            throw new DomainException(HttpStatusCode.BadRequest, "Cannot derive a key from the supplied title.");
        }

        if (baseKey.Length > MaxBaseKeyLength)
        {
            baseKey = baseKey[..MaxBaseKeyLength];
        }

        var key = baseKey;
        var suffix = 2;

        while (await _movieRepository.KeyExistsAsync(key, excludeId, cancellationToken))
        {
            key = $"{baseKey}-{suffix++}";
        }

        return key;
    }
}
