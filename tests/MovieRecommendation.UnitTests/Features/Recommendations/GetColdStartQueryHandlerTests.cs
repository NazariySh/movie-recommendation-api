using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetColdStart;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Recommendations;

public class GetColdStartQueryHandlerTests
{
    private readonly Mock<IRecommendationEngine> _engineMock;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IUserGenrePreferenceRepository> _preferencesMock;
    private readonly GetColdStartQueryHandler _handler;

    public GetColdStartQueryHandlerTests()
    {
        _engineMock = new Mock<IRecommendationEngine>();
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _cacheMock = new Mock<ICacheService>();
        _preferencesMock = new Mock<IUserGenrePreferenceRepository>();
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        _handler = new GetColdStartQueryHandler(
            _engineMock.Object,
            _movieRepositoryMock.Object,
            _cacheMock.Object,
            _preferencesMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UseColdStartCacheKeyAndFetchUserGenres()
    {
        var userId = Guid.NewGuid();
        var genreIds = new List<int> { 1, 4, 7 };
        _preferencesMock
            .Setup(p => p.GetGenreIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(genreIds);
        _engineMock
            .Setup(e => e.GetColdStartRecommendationsAsync(genreIds, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        await _handler.Handle(new GetColdStartQuery(userId, "en"), CancellationToken.None);

        _preferencesMock.Verify(p => p.GetGenreIdsAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(
            c => c.GetOrSetAsync(
                RecommendationCacheKeys.ColdStart(userId, 20, "en"),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MovieDto>>>>(),
                RecommendationCacheTtls.ColdStart,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoScores()
    {
        var userId = Guid.NewGuid();
        _preferencesMock
            .Setup(p => p.GetGenreIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());
        _engineMock
            .Setup(e => e.GetColdStartRecommendationsAsync(It.IsAny<IEnumerable<int>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        var result = await _handler.Handle(new GetColdStartQuery(userId, "en"), CancellationToken.None);

        result.Should().BeEmpty();
        _movieRepositoryMock.Verify(
            r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_AttachReasonAndPreserveOrder()
    {
        var userId = Guid.NewGuid();
        var movieA = Guid.NewGuid();
        var movieB = Guid.NewGuid();

        _preferencesMock
            .Setup(p => p.GetGenreIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });
        _engineMock
            .Setup(e => e.GetColdStartRecommendationsAsync(It.IsAny<IEnumerable<int>>(), 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>
            {
                new(movieB, 0.91, RecommendationReason.PopularInGenres),
                new(movieA, 0.85, RecommendationReason.PopularInGenres),
            });
        _movieRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieDto>
            {
                new() { Id = movieA, Title = "A" },
                new() { Id = movieB, Title = "B" },
            });

        var result = await _handler.Handle(new GetColdStartQuery(userId, "en"), CancellationToken.None);

        result.Select(m => m.Title).Should().ContainInOrder("B", "A");
        result.Should().AllSatisfy(m => m.RecommendationReason.Should().Be(RecommendationReason.PopularInGenres));
    }
}
