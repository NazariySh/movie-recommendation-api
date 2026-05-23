using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Search;
using MovieRecommendation.Application.Features.Search.Queries.GetSearchSuggestions;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Search;

public class GetSearchSuggestionsQueryHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<IArtistRepository> _artistRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetSearchSuggestionsQueryHandler _handler;

    public GetSearchSuggestionsQueryHandlerTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _artistRepositoryMock = new Mock<IArtistRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetSearchSuggestionsQueryHandler(
            _movieRepositoryMock.Object,
            _artistRepositoryMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_QueryShorterThanMinimum()
    {
        var result = await _handler.Handle(
            new GetSearchSuggestionsQuery("a", "en"),
            CancellationToken.None);

        result.Movies.Should().BeEmpty();
        result.Artists.Should().BeEmpty();
        _cacheMock.Verify(
            c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<SearchSuggestionsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_QueryIsWhitespace()
    {
        var result = await _handler.Handle(
            new GetSearchSuggestionsQuery("   ", "en"),
            CancellationToken.None);

        result.Movies.Should().BeEmpty();
        result.Artists.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_UseCacheKey_BuiltFromTrimmedQuery()
    {
        var expectedKey = SearchCacheKeys.Suggestions("en", "matrix", 7);
        var cached = new SearchSuggestionsDto
        {
            Movies = [new MovieSuggestionDto { Id = Guid.NewGuid(), Title = "The Matrix" }],
            Artists = [],
        };

        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<SearchSuggestionsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _handler.Handle(
            new GetSearchSuggestionsQuery("  matrix  ", "en", 7),
            CancellationToken.None);

        result.Should().BeSameAs(cached);
    }

    [Fact]
    public async Task Handle_Should_HydrateFromBothRepositories_OnCacheMiss()
    {
        var movies = new List<MovieSuggestionDto>
        {
            new() { Id = Guid.NewGuid(), Title = "The Matrix" },
        };
        var artists = new List<ArtistSuggestionDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Keanu Reeves" },
        };

        _movieRepositoryMock
            .Setup(r => r.SearchSuggestionsAsync("matrix", null, 5, "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(movies);
        _artistRepositoryMock
            .Setup(r => r.SearchSuggestionsAsync("matrix", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(artists);

        // Simulate a cache miss: forward to the factory.
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<SearchSuggestionsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<SearchSuggestionsDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var result = await _handler.Handle(
            new GetSearchSuggestionsQuery("matrix", "en"),
            CancellationToken.None);

        result.Movies.Should().BeSameAs(movies);
        result.Artists.Should().BeSameAs(artists);
    }
}
