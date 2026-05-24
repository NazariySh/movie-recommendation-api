using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetPopular;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.UnitTests.Features.Recommendations;

public class GetPopularQueryHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetPopularQueryHandler _handler;

    public GetPopularQueryHandlerTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _cacheMock = new Mock<ICacheService>();
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        _handler = new GetPopularQueryHandler(_movieRepositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UseCacheKeyIncludingTypeGenreCountAndLang()
    {
        _movieRepositoryMock
            .Setup(r => r.GetPopularIdsAsync(
                It.IsAny<TitleType?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        await _handler.Handle(
            new GetPopularQuery("en", TitleType.Series, "drama", 30),
            CancellationToken.None);

        _cacheMock.Verify(
            c => c.GetOrSetAsync(
                RecommendationCacheKeys.Popular(TitleType.Series, "drama", 30, "en"),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>>(),
                RecommendationCacheTtls.Popular,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PreserveRepositoryOrder()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();

        _movieRepositoryMock
            .Setup(r => r.GetPopularIdsAsync(
                null, null, It.IsAny<int>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { idB, idA, idC });

        // Repository hydrate returns out-of-order; handler must reorder.
        _movieRepositoryMock
            .Setup(r => r.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                "en",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieDto>
            {
                new() { Id = idC, Title = "C" },
                new() { Id = idA, Title = "A" },
                new() { Id = idB, Title = "B" },
            });

        var result = await _handler.Handle(new GetPopularQuery("en"), CancellationToken.None);

        result.Select(m => m.Title).Should().ContainInOrder("B", "A", "C");
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoIds()
    {
        _movieRepositoryMock
            .Setup(r => r.GetPopularIdsAsync(
                It.IsAny<TitleType?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var result = await _handler.Handle(new GetPopularQuery("en"), CancellationToken.None);

        result.Should().BeEmpty();
        _movieRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
