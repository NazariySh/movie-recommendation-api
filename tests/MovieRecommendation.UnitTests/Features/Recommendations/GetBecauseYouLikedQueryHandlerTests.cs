using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetBecauseYouLiked;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Recommendations;

public class GetBecauseYouLikedQueryHandlerTests
{
    private readonly Mock<IRecommendationEngine> _engineMock;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetBecauseYouLikedQueryHandler _handler;

    public GetBecauseYouLikedQueryHandlerTests()
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

        _handler = new GetBecauseYouLikedQueryHandler(
            _engineMock.Object,
            _movieRepositoryMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CallSimilar_WhenAnonymous()
    {
        var movieId = Guid.NewGuid();
        _engineMock
            .Setup(e => e.GetSimilarMoviesAsync(movieId, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        await _handler.Handle(
            new GetBecauseYouLikedQuery(movieId, "en", Guid.Empty),
            CancellationToken.None);

        _engineMock.Verify(e => e.GetSimilarMoviesAsync(movieId, 12, It.IsAny<CancellationToken>()), Times.Once);
        _engineMock.Verify(
            e => e.GetBecauseYouWatchedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_CallBecauseYouWatched_WhenAuthenticated()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _engineMock
            .Setup(e => e.GetBecauseYouWatchedAsync(userId, movieId, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        await _handler.Handle(
            new GetBecauseYouLikedQuery(movieId, "en", userId),
            CancellationToken.None);

        _engineMock.Verify(e => e.GetBecauseYouWatchedAsync(userId, movieId, 12, It.IsAny<CancellationToken>()), Times.Once);
        _engineMock.Verify(
            e => e.GetSimilarMoviesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_UseBecauseCacheKey()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var expectedKey = RecommendationCacheKeys.Because(movieId, userId, 12, "en");
        _engineMock
            .Setup(e => e.GetBecauseYouWatchedAsync(userId, movieId, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        await _handler.Handle(new GetBecauseYouLikedQuery(movieId, "en", userId), CancellationToken.None);

        _cacheMock.Verify(
            c => c.GetOrSetAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>>(),
                RecommendationCacheTtls.Because,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_AttachReasonAndPreserveOrder()
    {
        var userId = Guid.NewGuid();
        var sourceMovie = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        _engineMock
            .Setup(e => e.GetBecauseYouWatchedAsync(userId, sourceMovie, 12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>
            {
                new(b, 0.91, RecommendationReason.BecauseWatched),
                new(a, 0.82, RecommendationReason.BecauseWatched),
            });
        _movieRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieDto>
            {
                new() { Id = a, Title = "A" },
                new() { Id = b, Title = "B" },
            });

        var result = await _handler.Handle(new GetBecauseYouLikedQuery(sourceMovie, "en", userId), CancellationToken.None);

        result.Select(m => m.Title).Should().ContainInOrder("B", "A");
        result.Should().AllSatisfy(m => m.RecommendationReason.Should().Be(RecommendationReason.BecauseWatched));
    }
}
