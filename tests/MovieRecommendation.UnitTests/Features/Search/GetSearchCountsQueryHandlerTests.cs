using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Features.Search.Queries.GetSearchCounts;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.UnitTests.Features.Search;

public class GetSearchCountsQueryHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<IArtistRepository> _artistRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetSearchCountsQueryHandler _handler;

    public GetSearchCountsQueryHandlerTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _artistRepositoryMock = new Mock<IArtistRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetSearchCountsQueryHandler(
            _movieRepositoryMock.Object,
            _artistRepositoryMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_QueryIsWhitespace()
    {
        var result = await _handler.Handle(
            new GetSearchCountsQuery("   ", "en"),
            CancellationToken.None);

        result.Movies.Should().Be(0);
        result.Series.Should().Be(0);
        result.Artists.Should().Be(0);
        result.Total.Should().Be(0);
        _cacheMock.Verify(
            c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<SearchCountsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_UseCacheKey_BuiltFromTrimmedQuery()
    {
        var expectedKey = SearchCacheKeys.Counts("en", "matrix");
        var cached = new SearchCountsDto { Movies = 7, Series = 2, Artists = 4 };

        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<SearchCountsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _handler.Handle(
            new GetSearchCountsQuery("  matrix  ", "en"),
            CancellationToken.None);

        result.Should().BeSameAs(cached);
        result.Total.Should().Be(13);
    }

    [Fact]
    public async Task Handle_Should_QueryAllThreeRepositoryCalls_OnCacheMiss()
    {
        _movieRepositoryMock
            .Setup(r => r.CountSearchMatchesAsync("matrix", TitleType.Movie, "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _movieRepositoryMock
            .Setup(r => r.CountSearchMatchesAsync("matrix", TitleType.Series, "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _artistRepositoryMock
            .Setup(r => r.CountSearchMatchesAsync("matrix", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<SearchCountsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<SearchCountsDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var result = await _handler.Handle(
            new GetSearchCountsQuery("matrix", "en"),
            CancellationToken.None);

        result.Movies.Should().Be(5);
        result.Series.Should().Be(1);
        result.Artists.Should().Be(2);
        result.Total.Should().Be(8);
    }
}
