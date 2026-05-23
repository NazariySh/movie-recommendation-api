using FluentAssertions;
using Moq;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Reviews.Queries.GetReviewReplies;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class GetReviewRepliesQueryHandlerTests
{
    private readonly Mock<IMovieReviewRepository> _reviewRepositoryMock;
    private readonly GetReviewRepliesQueryHandler _handler;

    public GetReviewRepliesQueryHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IMovieReviewRepository>();
        _handler = new GetReviewRepliesQueryHandler(_reviewRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_DelegateToRepositoryWithViewerId()
    {
        var reviewId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        IReadOnlyList<MovieReviewDto> replies = new List<MovieReviewDto>
        {
            new() { Id = Guid.NewGuid(), Body = "Reply 1" },
            new() { Id = Guid.NewGuid(), Body = "Reply 2" },
        };

        _reviewRepositoryMock
            .Setup(r => r.GetRepliesAsync(reviewId, viewerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(replies);

        var result = await _handler.Handle(
            new GetReviewRepliesQuery(reviewId, viewerId),
            CancellationToken.None);

        result.Should().BeSameAs(replies);
        _reviewRepositoryMock.Verify(
            r => r.GetRepliesAsync(reviewId, viewerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
