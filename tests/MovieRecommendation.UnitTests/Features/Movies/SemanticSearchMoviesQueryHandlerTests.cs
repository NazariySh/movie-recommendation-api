using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class SemanticSearchMoviesQueryHandlerTests
{
    private readonly Mock<ISearchEngine> _searchEngineMock;
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly SemanticSearchMoviesQueryHandler _handler;

    public SemanticSearchMoviesQueryHandlerTests()
    {
        _searchEngineMock = new Mock<ISearchEngine>();
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _cacheMock = new Mock<ICacheService>();
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<SemanticSearchMoviesResult>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<SemanticSearchMoviesResult>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        _handler = new SemanticSearchMoviesQueryHandler(_searchEngineMock.Object, _movieRepositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_QueryBlank()
    {
        var result = await _handler.Handle(
            new SemanticSearchMoviesQuery("   ", "en", 20, 0.7),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        _searchEngineMock.Verify(
            s => s.SemanticSearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoScoredResults()
    {
        _searchEngineMock
            .Setup(s => s.SemanticSearchAsync("space opera", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>());

        var result = await _handler.Handle(
            new SemanticSearchMoviesQuery("space opera", "en", 20, 0.7),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        _movieRepositoryMock.Verify(
            r => r.GetListItemsByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_FilterOutBelowMinScore()
    {
        var lowId = Guid.NewGuid();
        var midId = Guid.NewGuid();
        var highId = Guid.NewGuid();
        _searchEngineMock
            .Setup(s => s.SemanticSearchAsync("space opera", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>
            {
                new(highId, 0.95),
                new(midId, 0.72),
                new(lowId, 0.50),
            });
        _movieRepositoryMock
            .Setup(r => r.GetListItemsByIdsAsync(
                It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 2),
                "en",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieListItemDto>
            {
                new() { Id = highId, Title = "High", AverageRating = 8.0m },
                new() { Id = midId, Title = "Mid", AverageRating = 6.5m },
            });

        var result = await _handler.Handle(
            new SemanticSearchMoviesQuery("space opera", "en", 20, 0.7),
            CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().NotContain(i => i.Id == lowId);
    }

    [Fact]
    public async Task Handle_Should_PreserveSearchEngineOrder_And_CarrySimilarityScore()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();
        _searchEngineMock
            .Setup(s => s.SemanticSearchAsync("space opera", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScoredMovie>
            {
                new(idB, 0.91),
                new(idA, 0.88),
                new(idC, 0.85),
            });
        
        _movieRepositoryMock
            .Setup(r => r.GetListItemsByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MovieListItemDto>
            {
                new() { Id = idC, Title = "C" },
                new() { Id = idA, Title = "A" },
                new() { Id = idB, Title = "B" },
            });

        var result = await _handler.Handle(
            new SemanticSearchMoviesQuery("space opera", "en", 20, 0.7),
            CancellationToken.None);

        result.Items.Select(i => i.Title).Should().ContainInOrder("B", "A", "C");
        result.Items[0].SimilarityScore.Should().Be(0.91);
        result.Items[1].SimilarityScore.Should().Be(0.88);
        result.Items[2].SimilarityScore.Should().Be(0.85);
    }
}
