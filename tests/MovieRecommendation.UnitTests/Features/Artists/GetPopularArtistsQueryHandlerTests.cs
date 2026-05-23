using FluentAssertions;
using MovieRecommendation.Application.Features.Artists.Queries.GetPopularArtists;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class GetPopularArtistsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetPopularArtistsQueryHandler _handler;

    public GetPopularArtistsQueryHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        _handler = new GetPopularArtistsQueryHandler(artistRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoArtists()
    {
        var result = await _handler.Handle(new GetPopularArtistsQuery(10), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ExcludeArtistsWithNoCast()
    {
        var orphan = TestData.Person(name: "Orphan", slug: "orphan");
        var cast = TestData.Person(name: "Working Actor", slug: "actor");
        var movie = TestData.Movie();
        _dbContext.People.Add(orphan);
        _dbContext.People.Add(cast);
        _dbContext.Movies.Add(movie);
        _dbContext.MovieCasts.Add(TestData.Cast(movie.Id, cast.Id));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetPopularArtistsQuery(10), CancellationToken.None);

        result.Should().ContainSingle().Which.Name.Should().Be("Working Actor");
    }

    [Fact]
    public async Task Handle_Should_OrderByMovieCountDescending_AndRespectLimit()
    {
        var movie = TestData.Movie();
        var movie2 = TestData.Movie(title: "Other", key: "other");
        var oneRole = TestData.Person(name: "One Role", slug: "one");
        var twoRoles = TestData.Person(name: "Two Roles", slug: "two");

        _dbContext.People.Add(oneRole);
        _dbContext.People.Add(twoRoles);
        _dbContext.Movies.Add(movie);
        _dbContext.Movies.Add(movie2);
        _dbContext.MovieCasts.Add(TestData.Cast(movie.Id, oneRole.Id));
        _dbContext.MovieCasts.Add(TestData.Cast(movie.Id, twoRoles.Id, role: "acting"));
        _dbContext.MovieCasts.Add(TestData.Cast(movie2.Id, twoRoles.Id, role: "directing"));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetPopularArtistsQuery(1), CancellationToken.None);

        result.Should().ContainSingle().Which.Name.Should().Be("Two Roles");
    }

    public void Dispose() => _dbContext.Dispose();
}
