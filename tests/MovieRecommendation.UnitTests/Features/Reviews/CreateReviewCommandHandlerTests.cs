using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Reviews.Commands.CreateReview;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class CreateReviewCommandHandlerTests : IDisposable
{
    private const int DailySubmitLimit = 10;

    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly CreateReviewCommandHandler _handler;

    public CreateReviewCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var reviewRepository = new MovieReviewRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new CreateReviewCommandHandler(
            reviewRepository,
            unitOfWork,
            TestMapperFactory.Create(),
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_PersistReviewAndReturnDto_When_PayloadIsValid()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        var command = new CreateReviewCommand(
            userId,
            movieId,
            new CreateMovieReviewDto
            {
                Body = "Cinematography was stunning.",
                IsSpoiler = false,
                Score = 9m,
            });

        var dto = await _handler.Handle(command, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Body.Should().Be("Cinematography was stunning.");
        (await _dbContext.MovieReviews.CountAsync(r => r.UserId == userId && r.MovieId == movieId))
            .Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ThrowTooManyRequests_When_UserAlreadySubmittedDailyLimit()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        for (var i = 0; i < DailySubmitLimit; i++)
        {
            _dbContext.MovieReviews.Add(TestData.Review(
                userId,
                movieId,
                body: $"earlier review {i}",
                createdAt: DateTime.UtcNow.AddMinutes(-i)));
        }
        await _dbContext.SaveChangesAsync();

        var command = new CreateReviewCommand(
            userId,
            movieId,
            new CreateMovieReviewDto { Body = "b" });

        var act = () => _handler.Handle(command, CancellationToken.None);

        var assertion = await act.Should().ThrowAsync<DomainException>();
        assertion.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Handle_Should_NotCountSubmissionsOlderThan24Hours_When_CheckingDailyLimit()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        for (var i = 0; i < DailySubmitLimit; i++)
        {
            _dbContext.MovieReviews.Add(TestData.Review(
                userId,
                movieId,
                body: $"old review {i}",
                createdAt: DateTime.UtcNow.AddDays(-2)));
        }
        await _dbContext.SaveChangesAsync();

        var command = new CreateReviewCommand(
            userId,
            movieId,
            new CreateMovieReviewDto { Body = "fresh take" });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_Should_InvalidateMovieReviewsCache_When_HandlerSucceeds()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new CreateReviewCommand(
                userId,
                movieId,
                new CreateMovieReviewDto { Body = "b" }),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(ReviewCacheKeys.ForMovie(movieId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
