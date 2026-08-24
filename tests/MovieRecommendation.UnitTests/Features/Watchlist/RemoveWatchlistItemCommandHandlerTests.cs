using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Watchlist.Commands.RemoveWatchlistItem;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Watchlist;

public class RemoveWatchlistItemCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly RemoveWatchlistItemCommandHandler _handler;

    public RemoveWatchlistItemCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var watchlistRepository = new WatchlistRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new RemoveWatchlistItemCommandHandler(
            watchlistRepository,
            unitOfWork,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_EntryDoesNotExist()
    {
        var command = new RemoveWatchlistItemCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteWatchlistItem_When_EntryExists()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(userId, movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new RemoveWatchlistItemCommand(userId, movieId), CancellationToken.None);

        var stillExists = await _dbContext.WatchlistItems.AnyAsync(w => w.UserId == userId && w.MovieId == movieId);
        stillExists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_InvalidateRecommendationCacheForUser_When_HandlerSucceeds()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(userId, movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new RemoveWatchlistItemCommand(userId, movieId), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
