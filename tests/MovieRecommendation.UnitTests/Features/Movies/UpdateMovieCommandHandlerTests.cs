using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class UpdateMovieCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly UpdateMovieCommandHandler _handler;

    public UpdateMovieCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var movieRepository = new MovieRepository(_dbContext, mapperConfig);
        var keyGenerator = new MovieKeyGenerator(movieRepository);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new UpdateMovieCommandHandler(
            movieRepository,
            keyGenerator,
            unitOfWork,
            TestMapperFactory.Create(),
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_MovieMissing()
    {
        var act = () => _handler.Handle(
            new UpdateMovieCommand(Guid.NewGuid(), NewDto()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateFields_And_InvalidateMovieCaches()
    {
        var movie = TestData.Movie(title: "Original", key: "original");
        _dbContext.Movies.Add(movie);
        await _dbContext.SaveChangesAsync();

        var dto = NewDto(originalTitle: "Renamed");

        await _handler.Handle(new UpdateMovieCommand(movie.Id, dto), CancellationToken.None);

        var reloaded = await _dbContext.Movies.SingleAsync(m => m.Id == movie.Id);
        reloaded.OriginalTitle.Should().Be("Renamed");
        reloaded.Key.Should().Be("renamed");
        reloaded.UpdatedAt.Should().NotBeNull();

        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.ListPrefix), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.DetailFor(movie.Id)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.SimilarFor(movie.Id)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_AppendSuffix_When_RenamedKeyCollides()
    {
        var other = TestData.Movie(title: "Other", key: "renamed");
        var subject = TestData.Movie(title: "Subject", key: "subject");
        _dbContext.Movies.Add(other);
        _dbContext.Movies.Add(subject);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateMovieCommand(subject.Id, NewDto(originalTitle: "Renamed")),
            CancellationToken.None);

        var reloaded = await _dbContext.Movies.SingleAsync(m => m.Id == subject.Id);
        reloaded.Key.Should().Be("renamed-2");
    }

    private static UpdateMovieDto NewDto(string originalTitle = "Some Movie") => new()
    {
        OriginalTitle = originalTitle,
        OriginalLang = "en",
        Type = TitleType.Movie,
    };

    public void Dispose() => _dbContext.Dispose();
}
