using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Ratings.Commands.DeleteRating;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Ratings;

public class DeleteRatingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly DeleteRatingCommandHandler _handler;

    public DeleteRatingCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var ratingRepository = new RatingRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new DeleteRatingCommandHandler(
            ratingRepository,
            unitOfWork,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFoundException_When_UserHasNoRatingForMovie()
    {
        var command = new DeleteRatingCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteRating_When_RatingExists()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.Ratings.Add(TestData.Rating(userId, movieId, score: 9m));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new DeleteRatingCommand(userId, movieId), CancellationToken.None);

        var exists = await _dbContext.Ratings.AnyAsync(r => r.UserId == userId && r.MovieId == movieId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_RecomputeMovieAggregatesToZero_When_LastRatingDeleted()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        var movie = TestData.Movie(id: movieId);
        movie.AverageRating = 9m;
        movie.RatingsCount = 1;
        _dbContext.Movies.Add(movie);
        _dbContext.Ratings.Add(TestData.Rating(userId, movieId, score: 9m));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new DeleteRatingCommand(userId, movieId), CancellationToken.None);

        var refreshed = await _dbContext.Movies.SingleAsync(m => m.Id == movieId);
        refreshed.AverageRating.Should().Be(0m);
        refreshed.RatingsCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_InvalidateMovieAndRecommendationCaches_When_HandlerSucceeds()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        _dbContext.Ratings.Add(TestData.Rating(userId, movieId));
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new DeleteRatingCommand(userId, movieId), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.ListPrefix), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.DetailFor(movieId)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
