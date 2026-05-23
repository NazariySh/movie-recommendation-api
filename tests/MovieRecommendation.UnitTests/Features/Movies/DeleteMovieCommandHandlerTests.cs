using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Movies.Commands.DeleteMovie;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class DeleteMovieCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly DeleteMovieCommandHandler _handler;

    public DeleteMovieCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var movieRepository = new MovieRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new DeleteMovieCommandHandler(movieRepository, unitOfWork, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_MovieMissing()
    {
        var act = () => _handler.Handle(new DeleteMovieCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_SoftDelete_And_InvalidateCaches()
    {
        var movie = TestData.Movie();
        _dbContext.Movies.Add(movie);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new DeleteMovieCommand(movie.Id), CancellationToken.None);

        var reloaded = await _dbContext.Movies.SingleAsync(m => m.Id == movie.Id);
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.UpdatedAt.Should().NotBeNull();

        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.ListPrefix), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.DetailFor(movie.Id)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.SimilarFor(movie.Id)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
