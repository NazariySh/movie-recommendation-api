using System.Net;
using MovieRecommendation.Application.Common;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Artists.Common;

public interface IArtistSlugGenerator
{
    Task<string> GenerateUniqueAsync(string name, Guid? excludeId, CancellationToken cancellationToken);
}

public class ArtistSlugGenerator : IArtistSlugGenerator
{
    private readonly IArtistRepository _artistRepository;

    public ArtistSlugGenerator(IArtistRepository artistRepository)
    {
        _artistRepository = artistRepository;
    }

    public async Task<string> GenerateUniqueAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify.ToSlug(name);
        if (string.IsNullOrEmpty(baseSlug))
        {
            throw new DomainException(HttpStatusCode.BadRequest, "Cannot derive a slug from the supplied name.");
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _artistRepository.SlugExistsAsync(slug, excludeId, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
