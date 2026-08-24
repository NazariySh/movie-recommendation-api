using MovieRecommendation.Application.Interfaces;

namespace MovieRecommendation.UnitTests.Infrastructure;

public sealed class FakeImageMirror : IImageMirrorService
{
    public Task<string?> MirrorAsync(
        string? sourceUrl,
        string pathWithoutExtension,
        CancellationToken cancellationToken = default)
        => Task.FromResult(sourceUrl);

    public Task<string> StoreUploadAsync(
        Stream content,
        string contentType,
        string pathWithoutExtension,
        CancellationToken cancellationToken = default)
        => Task.FromResult($"https://blob.test/{pathWithoutExtension}");
}
