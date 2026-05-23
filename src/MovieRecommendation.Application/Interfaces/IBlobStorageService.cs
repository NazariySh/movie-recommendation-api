namespace MovieRecommendation.Application.Interfaces;

public interface IBlobStorageService
{
    bool IsConfigured { get; }

    Task<string> UploadAsync(
        string path,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string path, CancellationToken cancellationToken = default);

    Task<string> CopyFromUrlAsync(
        string sourceUrl,
        string path,
        CancellationToken cancellationToken = default);
}
