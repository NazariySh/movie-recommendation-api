using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Reviews.Commands.ToggleHelpful;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class ToggleHelpfulCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly ToggleHelpfulCommandHandler _handler;

    public ToggleHelpfulCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();
        _dateTimeProvider = new FakeDateTimeProvider();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var reviewRepository = new MovieReviewRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new ToggleHelpfulCommandHandler(
            reviewRepository,
            unitOfWork,
            _cacheMock.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ReviewDoesNotExist()
    {
        var act = () => _handler.Handle(
            new ToggleHelpfulCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbiddenException_When_AuthorMarksOwnReviewHelpful()
    {
        var authorId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: authorId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(
            new ToggleHelpfulCommand(review.Id, authorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_Should_AddVoteAndIncrementCount_When_VoterHasNotVotedBefore()
    {
        var authorId = Guid.NewGuid();
        var voterId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: voterId, userName: "voter"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId, helpfulCount: 3);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new ToggleHelpfulCommand(review.Id, voterId),
            CancellationToken.None);

        result.MarkedHelpful.Should().BeTrue();
        result.HelpfulCount.Should().Be(4);

        var voteExists = await _dbContext.ReviewHelpfulVotes.AnyAsync(
            v => v.UserId == voterId && v.ReviewId == review.Id);
        voteExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_RemoveVoteAndDecrementCount_When_VoterAlreadyVoted()
    {
        var authorId = Guid.NewGuid();
        var voterId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: voterId, userName: "voter"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId, helpfulCount: 5);
        _dbContext.MovieReviews.Add(review);
        _dbContext.ReviewHelpfulVotes.Add(new ReviewHelpfulVote
        {
            UserId = voterId,
            ReviewId = review.Id,
            CreatedAt = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new ToggleHelpfulCommand(review.Id, voterId),
            CancellationToken.None);

        result.MarkedHelpful.Should().BeFalse();
        result.HelpfulCount.Should().Be(4);

        var voteExists = await _dbContext.ReviewHelpfulVotes.AnyAsync(
            v => v.UserId == voterId && v.ReviewId == review.Id);
        voteExists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ClampHelpfulCountAtZero_When_RemovingVoteWhileCountIsZero()
    {
        var authorId = Guid.NewGuid();
        var voterId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: voterId, userName: "voter"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId, helpfulCount: 0);
        _dbContext.MovieReviews.Add(review);
        _dbContext.ReviewHelpfulVotes.Add(new ReviewHelpfulVote
        {
            UserId = voterId,
            ReviewId = review.Id,
            CreatedAt = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new ToggleHelpfulCommand(review.Id, voterId),
            CancellationToken.None);

        result.HelpfulCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_InvalidateMovieReviewsCache_When_HandlerSucceeds()
    {
        var authorId = Guid.NewGuid();
        var voterId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: voterId, userName: "voter"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new ToggleHelpfulCommand(review.Id, voterId),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(ReviewCacheKeys.ForMovie(movieId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
