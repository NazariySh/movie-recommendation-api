using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Reviews.Queries.GetMovieReviews;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class GetMovieReviewsQueryHandlerTests
{
    private readonly Mock<IMovieReviewRepository> _reviewRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetMovieReviewsQueryHandler _handler;

    public GetMovieReviewsQueryHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IMovieReviewRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetMovieReviewsQueryHandler(_reviewRepositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UseCacheKey_BuiltFromMovieReviewsPage()
    {
        var movieId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var query = new GetMovieReviewsQuery(
            movieId,
            new SearchReviewsDto { Sort = "newest", PageNumber = 2, PageSize = 10 },
            viewerId);
        var expectedKey = ReviewCacheKeys.MovieReviewsPage(movieId, viewerId, "newest", 2, 10);
        var page = new PagedList<MovieReviewDto>([new MovieReviewDto { Id = Guid.NewGuid(), Body = "x" }], 2, 10, 25);

        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<PagedList<MovieReviewDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeSameAs(page);
    }

    [Fact]
    public async Task Handle_Should_DelegateFactoryToRepository_OnCacheMiss()
    {
        var movieId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var dto = new SearchReviewsDto { Sort = "hottest", PageNumber = 1, PageSize = 5 };
        var fromRepo = new PagedList<MovieReviewDto>([], 1, 5, 0);

        _reviewRepositoryMock
            .Setup(r => r.GetForMovieAsync(movieId, dto, viewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fromRepo);

        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<PagedList<MovieReviewDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<PagedList<MovieReviewDto>>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var result = await _handler.Handle(new GetMovieReviewsQuery(movieId, dto, viewerId), CancellationToken.None);

        result.Should().BeSameAs(fromRepo);
        _reviewRepositoryMock.Verify(
            r => r.GetForMovieAsync(movieId, dto, viewerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
