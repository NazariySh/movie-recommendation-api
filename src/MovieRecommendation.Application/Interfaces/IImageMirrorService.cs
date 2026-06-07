namespace MovieRecommendation.Application.Interfaces;

public interface IImageMirrorService
{
    Task<string?> MirrorAsync(
        string? sourceUrl,
        string pathWithoutExtension,
        CancellationToken cancellationToken = default);

    Task<string> StoreUploadAsync(
        Stream content,
        string contentType,
        string pathWithoutExtension,
        CancellationToken cancellationToken = default);
}
