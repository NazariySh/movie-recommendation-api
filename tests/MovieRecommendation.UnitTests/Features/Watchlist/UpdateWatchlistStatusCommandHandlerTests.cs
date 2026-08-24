using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.Features.Watchlist.Commands.UpdateWatchlistStatus;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Watchlist;

public class UpdateWatchlistStatusCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly UpdateWatchlistStatusCommandHandler _handler;

    public UpdateWatchlistStatusCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();
        _dateTimeProvider = new FakeDateTimeProvider();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var watchlistRepository = new WatchlistRepository(_dbContext, mapperConfig);
        var watchHistoryRepository = new WatchHistoryRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new UpdateWatchlistStatusCommandHandler(
            watchlistRepository,
            watchHistoryRepository,
            unitOfWork,
            _cacheMock.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_NoWatchlistEntryForUserAndMovie()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        var command = new UpdateWatchlistStatusCommand(
            userId,
            movieId,
            new UpdateWatchlistStatusDto { Status = WatchlistStatus.Watching });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateStatusAndUpdatedAt_When_EntryExists()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 13, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(
            userId,
            movieId,
            status: WatchlistStatus.PlanToWatch,
            notes: "later"));
        await _dbContext.SaveChangesAsync();

        var command = new UpdateWatchlistStatusCommand(
            userId,
            movieId,
            new UpdateWatchlistStatusDto { Status = WatchlistStatus.Watching });

        await _handler.Handle(command, CancellationToken.None);

        var item = await _dbContext.WatchlistItems.SingleAsync(w => w.UserId == userId && w.MovieId == movieId);
        item.Status.Should().Be(WatchlistStatus.Watching);
        item.UpdatedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_NotOverwriteNotes_When_NotesInDtoIsNull()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(
            userId,
            movieId,
            status: WatchlistStatus.PlanToWatch,
            notes: "keep me"));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateWatchlistStatusCommand(
                userId,
                movieId,
                new UpdateWatchlistStatusDto { Status = WatchlistStatus.Watching, Notes = null }),
            CancellationToken.None);

        var item = await _dbContext.WatchlistItems.SingleAsync(w => w.UserId == userId && w.MovieId == movieId);
        item.Notes.Should().Be("keep me");
    }

    [Fact]
    public async Task Handle_Should_CreateWatchHistory_When_NewStatusIsCompleted()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 23, 30, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(
            userId,
            movieId,
            status: WatchlistStatus.Watching));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateWatchlistStatusCommand(
                userId,
                movieId,
                new UpdateWatchlistStatusDto { Status = WatchlistStatus.Completed }),
            CancellationToken.None);

        var history = await _dbContext.WatchHistory.SingleOrDefaultAsync(h => h.UserId == userId && h.MovieId == movieId);
        history.Should().NotBeNull();
        history!.WatchedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_InvalidateRecommendationCache_When_HandlerSucceeds()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(userId, movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateWatchlistStatusCommand(
                userId,
                movieId,
                new UpdateWatchlistStatusDto { Status = WatchlistStatus.Watching }),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
