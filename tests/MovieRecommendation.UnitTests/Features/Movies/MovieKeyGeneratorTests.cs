using System.Net;
using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class MovieKeyGeneratorTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly MovieKeyGenerator _generator;

    public MovieKeyGeneratorTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _generator = new MovieKeyGenerator(_movieRepositoryMock.Object);
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_UseRequestedKey_When_Provided()
    {
        _movieRepositoryMock
            .Setup(r => r.KeyExistsAsync("custom-key", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var key = await _generator.GenerateUniqueAsync(
            requestedKey: "custom-key",
            fallbackSource: "Some Title",
            excludeId: null,
            CancellationToken.None);

        key.Should().Be("custom-key");
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_FallBackToTitleSlug_When_RequestedKeyEmpty()
    {
        _movieRepositoryMock
            .Setup(r => r.KeyExistsAsync("the-matrix", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var key = await _generator.GenerateUniqueAsync(
            requestedKey: null,
            fallbackSource: "The Matrix",
            excludeId: null,
            CancellationToken.None);

        key.Should().Be("the-matrix");
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_AppendIncrementingSuffix_When_KeyTaken()
    {
        _movieRepositoryMock.Setup(r => r.KeyExistsAsync("the-matrix", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _movieRepositoryMock.Setup(r => r.KeyExistsAsync("the-matrix-2", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _movieRepositoryMock.Setup(r => r.KeyExistsAsync("the-matrix-3", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var key = await _generator.GenerateUniqueAsync(
            requestedKey: null,
            fallbackSource: "The Matrix",
            excludeId: null,
            CancellationToken.None);

        key.Should().Be("the-matrix-3");
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_PassExcludeIdThrough()
    {
        var excludeId = Guid.NewGuid();
        _movieRepositoryMock
            .Setup(r => r.KeyExistsAsync("the-matrix", excludeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var key = await _generator.GenerateUniqueAsync(null, "The Matrix", excludeId, CancellationToken.None);

        key.Should().Be("the-matrix");
        _movieRepositoryMock.Verify(
            r => r.KeyExistsAsync("the-matrix", excludeId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_Throw_When_TitleSlugifiesToEmpty()
    {
        var act = () => _generator.GenerateUniqueAsync(null, "!!!", null, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _movieRepositoryMock.Verify(
            r => r.KeyExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
