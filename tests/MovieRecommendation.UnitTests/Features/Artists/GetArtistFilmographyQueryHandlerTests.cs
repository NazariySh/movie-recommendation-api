using FluentAssertions;
using MovieRecommendation.Application.Features.Artists.Queries.GetArtistFilmography;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class GetArtistFilmographyQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetArtistFilmographyQueryHandler _handler;

    public GetArtistFilmographyQueryHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        _handler = new GetArtistFilmographyQueryHandler(artistRepository);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ArtistMissing()
    {
        var act = () => _handler.Handle(
            new GetArtistFilmographyQuery(Guid.NewGuid(), Role: null, Lang: "en"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ArtistSoftDeleted()
    {
        var artist = TestData.Person(isDeleted: true);
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(
            new GetArtistFilmographyQuery(artist.Id, Role: null, Lang: "en"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_ArtistHasNoCast()
    {
        var artist = TestData.Person();
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetArtistFilmographyQuery(artist.Id, Role: null, Lang: "en"),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnFilmographyForArtist()
    {
        var artist = TestData.Person();
        var movie = TestData.Movie(title: "Inception");
        _dbContext.People.Add(artist);
        _dbContext.Movies.Add(movie);
        _dbContext.MovieCasts.Add(TestData.Cast(movie.Id, artist.Id, role: "directing"));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetArtistFilmographyQuery(artist.Id, Role: null, Lang: "en"),
            CancellationToken.None);

        result.Should().ContainSingle()
            .Which.MovieTitle.Should().Be("Inception");
    }

    public void Dispose() => _dbContext.Dispose();
}
