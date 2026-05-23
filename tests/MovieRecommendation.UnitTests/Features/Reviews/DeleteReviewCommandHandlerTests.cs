using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Features.Reviews.Commands.DeleteReview;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class DeleteReviewCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly DeleteReviewCommandHandler _handler;

    public DeleteReviewCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();
        _dateTimeProvider = new FakeDateTimeProvider();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var reviewRepository = new MovieReviewRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new DeleteReviewCommandHandler(
            reviewRepository,
            unitOfWork,
            _cacheMock.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ReviewDoesNotExist()
    {
        var command = new DeleteReviewCommand(Guid.NewGuid(), Guid.NewGuid(), IsModerator: false);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbiddenException_When_NonAuthorNonModeratorDeletes()
    {
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: otherUserId, userName: "other"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(
            new DeleteReviewCommand(review.Id, otherUserId, IsModerator: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_Should_SoftDeleteReview_When_AuthorDeletes()
    {
        var authorId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 16, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: authorId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new DeleteReviewCommand(review.Id, authorId, IsModerator: false),
            CancellationToken.None);

        var stored = await _dbContext.MovieReviews.IgnoreQueryFilters().SingleAsync(r => r.Id == review.Id);
        stored.IsDeleted.Should().BeTrue();
        stored.UpdatedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_AllowModeratorToDelete_When_CallerIsNotTheAuthor()
    {
        var authorId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: moderatorId, userName: "mod"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new DeleteReviewCommand(review.Id, moderatorId, IsModerator: true),
            CancellationToken.None);

        var stored = await _dbContext.MovieReviews.IgnoreQueryFilters().SingleAsync(r => r.Id == review.Id);
        stored.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_SoftDeleteReplies_When_ParentReviewIsDeleted()
    {
        var authorId = Guid.NewGuid();
        var replyAuthorId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 16, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.AddRange(
            TestData.User(id: authorId),
            TestData.User(id: replyAuthorId, userName: "replyAuthor"));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        var reply = TestData.Review(replyAuthorId, movieId, body: "thanks", parentReviewId: review.Id);
        _dbContext.MovieReviews.AddRange(review, reply);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new DeleteReviewCommand(review.Id, authorId, IsModerator: false),
            CancellationToken.None);

        var replyStored = await _dbContext.MovieReviews.IgnoreQueryFilters().SingleAsync(r => r.Id == reply.Id);
        replyStored.IsDeleted.Should().BeTrue();
        replyStored.UpdatedAt.Should().Be(fixedNow);
    }

    public void Dispose() => _dbContext.Dispose();
}
