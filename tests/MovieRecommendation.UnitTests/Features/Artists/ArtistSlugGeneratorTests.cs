using System.Net;
using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class ArtistSlugGeneratorTests
{
    private readonly Mock<IArtistRepository> _artistRepositoryMock;
    private readonly ArtistSlugGenerator _generator;

    public ArtistSlugGeneratorTests()
    {
        _artistRepositoryMock = new Mock<IArtistRepository>();
        _generator = new ArtistSlugGenerator(_artistRepositoryMock.Object);
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_ReturnBaseSlug_When_Available()
    {
        _artistRepositoryMock
            .Setup(r => r.SlugExistsAsync("john-doe", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var slug = await _generator.GenerateUniqueAsync("John Doe", excludeId: null, CancellationToken.None);

        slug.Should().Be("john-doe");
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_AppendIncrementingSuffix_When_BaseTaken()
    {
        _artistRepositoryMock
            .Setup(r => r.SlugExistsAsync("john-doe", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _artistRepositoryMock
            .Setup(r => r.SlugExistsAsync("john-doe-2", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _artistRepositoryMock
            .Setup(r => r.SlugExistsAsync("john-doe-3", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var slug = await _generator.GenerateUniqueAsync("John Doe", excludeId: null, CancellationToken.None);

        slug.Should().Be("john-doe-3");
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_PassExcludeIdThrough_When_Provided()
    {
        var excludeId = Guid.NewGuid();
        _artistRepositoryMock
            .Setup(r => r.SlugExistsAsync("john-doe", excludeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var slug = await _generator.GenerateUniqueAsync("John Doe", excludeId, CancellationToken.None);

        slug.Should().Be("john-doe");
        _artistRepositoryMock.Verify(
            r => r.SlugExistsAsync("john-doe", excludeId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateUniqueAsync_Should_Throw_When_NameSlugifiesToEmpty()
    {
        var act = () => _generator.GenerateUniqueAsync("!!!", excludeId: null, CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _artistRepositoryMock.Verify(
            r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
