using System.Net;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Infrastructure.Services;

public class ImageMirrorService : IImageMirrorService
{
    private const string DefaultImageExtension = ".jpg";

    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<ImageMirrorService> _logger;

    public ImageMirrorService(IBlobStorageService blobStorage, ILogger<ImageMirrorService> logger)
    {
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<string?> MirrorAsync(
        string? sourceUrl,
        string pathWithoutExtension,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            return null;
        }

        if (!_blobStorage.IsConfigured)
        {
            return sourceUrl;
        }

        try
        {
            if (_blobStorage.OwnsUrl(sourceUrl))
            {
                return sourceUrl;
            }

            var extension = Path.GetExtension(new Uri(sourceUrl).AbsolutePath);
            if (string.IsNullOrEmpty(extension))
            {
                extension = DefaultImageExtension;
            }

            return await _blobStorage.CopyFromUrlAsync(sourceUrl, pathWithoutExtension + extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mirror image {SourceUrl}; falling back to source URL", sourceUrl);
            return sourceUrl;
        }
    }

    public async Task<string> StoreUploadAsync(
        Stream content,
        string contentType,
        string pathWithoutExtension,
        CancellationToken cancellationToken = default)
    {
        if (!_blobStorage.IsConfigured)
        {
            throw new DomainException(
                HttpStatusCode.ServiceUnavailable,
                "Image uploads are unavailable: blob storage is not configured.");
        }

        if (!AllowedContentTypes.TryGetValue(contentType, out var extension))
        {
            throw new DomainException(
                HttpStatusCode.UnsupportedMediaType,
                $"Unsupported content type '{contentType}'. Allowed: jpeg, png, webp.");
        }

        return await _blobStorage.UploadAsync(
            $"{pathWithoutExtension}.{extension}",
            content,
            contentType,
            cancellationToken);
    }
}
