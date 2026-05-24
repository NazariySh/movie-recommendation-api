using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetForYou;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Recommendations;

public class GetForYouQueryHandlerTests
{
    private readonly Mock<IRecommendationEngine> _engineMock;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetForYouQueryHandler _handler;

    public GetForYouQueryHandlerTests()
    {
        _engineMock = new Mock<IRecommendationEngine>();
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

        _handler = new GetForYouQueryHandler(
            _engineMock.Object,
            _movieRepositoryMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UseExpectedCacheKey()
    {
        var userId = Guid.NewGuid();
        var expectedKey = RecommendationCacheKeys.ForYou(userId, 20, "en");
        _engineMock
            .Setup(e => e.GetPersonalRecommendationsAsync(userId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        await _handler.Handle(new GetForYouQuery(userId, "en"), CancellationToken.None);

        _cacheMock.Verify(
            c => c.GetOrSetAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>>(),
                RecommendationCacheTtls.ForYou,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_EngineYieldsNothing()
    {
        var userId = Guid.NewGuid();
        _engineMock
            .Setup(e => e.GetPersonalRecommendationsAsync(userId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        var result = await _handler.Handle(new GetForYouQuery(userId, "en"), CancellationToken.None);

        result.Should().BeEmpty();
        _movieRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_PreserveEngineOrder_And_AttachReasonPerMovie()
    {
        var userId = Guid.NewGuid();
        var movieA = Guid.NewGuid();
        var movieB = Guid.NewGuid();
        var movieC = Guid.NewGuid();

        _engineMock
            .Setup(e => e.GetPersonalRecommendationsAsync(userId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>
            {
                new(movieB, 0.95, RecommendationReason.ForYou),
                new(movieA, 0.88, RecommendationReason.ForYou),
                new(movieC, 0.81, RecommendationReason.ForYou),
            });

        _movieRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieDto>
            {
                new() { Id = movieC, Title = "C" },
                new() { Id = movieA, Title = "A" },
                new() { Id = movieB, Title = "B" },
            });

        var result = await _handler.Handle(new GetForYouQuery(userId, "en"), CancellationToken.None);

        result.Select(m => m.Title).Should().ContainInOrder("B", "A", "C");
        result.Should().AllSatisfy(m => m.RecommendationReason.Should().Be(RecommendationReason.ForYou));
    }
}
