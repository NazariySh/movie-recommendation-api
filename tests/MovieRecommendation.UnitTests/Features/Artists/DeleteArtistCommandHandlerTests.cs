using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Features.Artists.Commands.DeleteArtist;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class DeleteArtistCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DeleteArtistCommandHandler _handler;

    public DeleteArtistCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new DeleteArtistCommandHandler(artistRepository, unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_ArtistDoesNotExist()
    {
        var act = () => _handler.Handle(new DeleteArtistCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_SoftDeleteArtist_When_NoMovieCastReferences()
    {
        var artist = TestData.Person();
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(new DeleteArtistCommand(artist.Id), CancellationToken.None);

        var reloaded = await _dbContext.People.IgnoreQueryFilters().SingleAsync(p => p.Id == artist.Id);
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_ThrowConflict_When_ArtistHasMovieCastReferences()
    {
        var artist = TestData.Person();
        var movie = TestData.Movie();
        _dbContext.People.Add(artist);
        _dbContext.Movies.Add(movie);
        _dbContext.MovieCasts.Add(TestData.Cast(movie.Id, artist.Id));
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(new DeleteArtistCommand(artist.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var reloaded = await _dbContext.People.SingleAsync(p => p.Id == artist.Id);
        reloaded.IsDeleted.Should().BeFalse();
    }

    public void Dispose() => _dbContext.Dispose();
}
