using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Reviews.Commands.UpdateReview;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class UpdateReviewCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly UpdateReviewCommandHandler _handler;

    public UpdateReviewCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();
        _dateTimeProvider = new FakeDateTimeProvider();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var reviewRepository = new MovieReviewRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new UpdateReviewCommandHandler(
            reviewRepository,
            unitOfWork,
            _cacheMock.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_ReviewDoesNotExist()
    {
        var command = new UpdateReviewCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateMovieReviewDto { Body = "edit" });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbiddenException_When_CurrentUserIsNotTheAuthor()
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

        var command = new UpdateReviewCommand(
            review.Id,
            otherUserId,
            new UpdateMovieReviewDto { Body = "trying to edit" });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateAllEditableFieldsAndUpdatedAt_When_AuthorEdits()
    {
        var authorId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 14, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: authorId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId, body: "Original body", score: 5m);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateReviewCommand(
                review.Id,
                authorId,
                new UpdateMovieReviewDto
                {
                    Body = "Updated body",
                    Score = 8m,
                    IsSpoiler = true,
                }),
            CancellationToken.None);

        var refreshed = await _dbContext.MovieReviews.SingleAsync(r => r.Id == review.Id);
        refreshed.Body.Should().Be("Updated body");
        refreshed.Score.Should().Be(8m);
        refreshed.IsSpoiler.Should().BeTrue();
        refreshed.UpdatedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_InvalidateMovieReviewsCache_When_HandlerSucceeds()
    {
        var authorId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: authorId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var review = TestData.Review(authorId, movieId);
        _dbContext.MovieReviews.Add(review);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateReviewCommand(review.Id, authorId, new UpdateMovieReviewDto { Body = "ok" }),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(ReviewCacheKeys.ForMovie(movieId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
