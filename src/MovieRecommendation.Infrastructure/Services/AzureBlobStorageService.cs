using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly Lazy<BlobContainerClient> _container;
    private readonly BlobStorageSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<BlobStorageSettings> options,
        IHttpClientFactory httpClientFactory,
        ILogger<AzureBlobStorageService> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(AzureBlobStorageService));

        _container = new Lazy<BlobContainerClient>(() =>
        {
            var serviceClient = new BlobServiceClient(_settings.ConnectionString);
            return serviceClient.GetBlobContainerClient(_settings.ContainerName);
        });
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ConnectionString);

    public async Task<string> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var container = _container.Value;
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(path);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return BuildPublicUrl(blob);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return _container.Value.GetBlobClient(path).DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<string> CopyFromUrlAsync(
        string sourceUrl,
        string path,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var container = _container.Value;
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(path);

        if (await blob.ExistsAsync(cancellationToken))
        {
            _logger.LogDebug("Blob {Path} already exists; skipping copy from {SourceUrl}", path, sourceUrl);
            return BuildPublicUrl(blob);
        }

        using var response = await _httpClient.GetAsync(sourceUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return await UploadAsync(path, stream, contentType, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("BlobStorageSettings:ConnectionString is not configured.");
        }
    }

    private string BuildPublicUrl(BlobClient blob)
    {
        if (string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
        {
            return blob.Uri.ToString();
        }

        var baseUrl = _settings.PublicBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{_settings.ContainerName}/{blob.Name}";
    }
}
