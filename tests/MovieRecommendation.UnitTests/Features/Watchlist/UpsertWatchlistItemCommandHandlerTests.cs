using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.Features.Watchlist.Commands.UpsertWatchlistItem;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Watchlist;

public class UpsertWatchlistItemCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly UpsertWatchlistItemCommandHandler _handler;

    public UpsertWatchlistItemCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();
        _dateTimeProvider = new FakeDateTimeProvider();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var watchlistRepository = new WatchlistRepository(_dbContext, mapperConfig);
        var watchHistoryRepository = new WatchHistoryRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new UpsertWatchlistItemCommandHandler(
            watchlistRepository,
            watchHistoryRepository,
            unitOfWork,
            _cacheMock.Object,
            _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_Should_AddNewWatchlistItem_When_NoExistingEntryForUserAndMovie()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        var command = new UpsertWatchlistItemCommand(
            userId,
            new UpsertWatchlistDto
            {
                MovieId = movieId,
                Status = WatchlistStatus.PlanToWatch,
                Notes = "Recommended by Alex",
            });

        await _handler.Handle(command, CancellationToken.None);

        var item = await _dbContext.WatchlistItems
            .SingleOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);
        item.Should().NotBeNull();
        item!.Status.Should().Be(WatchlistStatus.PlanToWatch);
        item.Notes.Should().Be("Recommended by Alex");
    }

    [Fact]
    public async Task Handle_Should_UpdateStatusAndNotesAndUpdatedAt_When_EntryAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 9, 30, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(
            userId,
            movieId,
            status: WatchlistStatus.PlanToWatch,
            notes: "old notes"));
        await _dbContext.SaveChangesAsync();

        var command = new UpsertWatchlistItemCommand(
            userId,
            new UpsertWatchlistDto
            {
                MovieId = movieId,
                Status = WatchlistStatus.Watching,
                Notes = "started this evening",
            });

        await _handler.Handle(command, CancellationToken.None);

        var item = await _dbContext.WatchlistItems
            .SingleAsync(w => w.UserId == userId && w.MovieId == movieId);
        item.Status.Should().Be(WatchlistStatus.Watching);
        item.Notes.Should().Be("started this evening");
        item.UpdatedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_UpsertWatchHistoryWithProvidedNow_When_StatusIsCompleted()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var fixedNow = new DateTime(2026, 5, 16, 21, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow = fixedNow;

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        var command = new UpsertWatchlistItemCommand(
            userId,
            new UpsertWatchlistDto { MovieId = movieId, Status = WatchlistStatus.Completed });

        await _handler.Handle(command, CancellationToken.None);

        var history = await _dbContext.WatchHistory
            .SingleOrDefaultAsync(h => h.UserId == userId && h.MovieId == movieId);
        history.Should().NotBeNull();
        history!.WatchedAt.Should().Be(fixedNow);
    }

    [Fact]
    public async Task Handle_Should_NotCreateWatchHistory_When_StatusIsNotCompleted()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        var command = new UpsertWatchlistItemCommand(
            userId,
            new UpsertWatchlistDto { MovieId = movieId, Status = WatchlistStatus.Dropped });

        await _handler.Handle(command, CancellationToken.None);

        var historyExists = await _dbContext.WatchHistory
            .AnyAsync(h => h.UserId == userId && h.MovieId == movieId);
        historyExists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_InvalidateRecommendationCacheForUser_When_HandlerSucceeds()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpsertWatchlistItemCommand(
                userId,
                new UpsertWatchlistDto { MovieId = movieId, Status = WatchlistStatus.Watching }),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
