using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.Features.Ratings.Commands.UpsertRating;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Ratings;

public class UpsertRatingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly UpsertRatingCommandHandler _handler;

    public UpsertRatingCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();
        _dateTimeProvider = new FakeDateTimeProvider();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var ratingRepository = new RatingRepository(_dbContext, mapperConfig);
        var watchHistoryRepository = new WatchHistoryRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new UpsertRatingCommandHandler(
            ratingRepository,
            watchHistoryRepository,
            unitOfWork,
            TestMapperFactory.Create(),
            _cacheMock.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_CreateNewRating_When_NoExistingRatingForUserAndMovie()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        var dto = await _handler.Handle(
            new UpsertRatingCommand(userId, movieId, new UpsertRatingDto { Value = 8m }),
            CancellationToken.None);

        dto.MovieId.Should().Be(movieId);
        dto.Score.Should().Be(8m);

        var stored = await _dbContext.Ratings.SingleAsync(r => r.UserId == userId && r.MovieId == movieId);
        stored.Score.Should().Be(8m);
    }

    [Fact]
    public async Task Handle_Should_UpdateScoreAndUpdatedAt_When_RatingAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 17, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.Ratings.Add(TestData.Rating(userId, movieId, score: 5m));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpsertRatingCommand(userId, movieId, new UpsertRatingDto { Value = 9m }),
            CancellationToken.None);

        var stored = await _dbContext.Ratings.SingleAsync(r => r.UserId == userId && r.MovieId == movieId);
        stored.Score.Should().Be(9m);
        stored.UpdatedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_RecomputeMovieAggregates_When_RatingChanges()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpsertRatingCommand(userId, movieId, new UpsertRatingDto { Value = 8m }),
            CancellationToken.None);

        var movie = await _dbContext.Movies.SingleAsync(m => m.Id == movieId);
        movie.AverageRating.Should().Be(8m);
        movie.RatingsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_CreateWatchHistoryEntry_When_RatingMeetsAutoWatchThreshold()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 19, 45, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpsertRatingCommand(userId, movieId, new UpsertRatingDto { Value = 7m }),
            CancellationToken.None);

        var history = await _dbContext.WatchHistory.SingleOrDefaultAsync(h => h.UserId == userId && h.MovieId == movieId);
        history.Should().NotBeNull();
        history!.WatchedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_NotCreateWatchHistoryEntry_When_RatingBelowAutoWatchThreshold()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpsertRatingCommand(userId, movieId, new UpsertRatingDto { Value = 6.5m }),
            CancellationToken.None);

        var exists = await _dbContext.WatchHistory.AnyAsync(h => h.UserId == userId && h.MovieId == movieId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_InvalidateMovieAndRecommendationCaches_When_HandlerSucceeds()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpsertRatingCommand(userId, movieId, new UpsertRatingDto { Value = 5m }),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.ListPrefix), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.DetailFor(movieId)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
