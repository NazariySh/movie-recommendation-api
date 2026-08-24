using FluentAssertions;
using MovieRecommendation.Application.Features.Artists.Queries.GetArtistById;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class GetArtistByIdQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetArtistByIdQueryHandler _handler;

    public GetArtistByIdQueryHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        _handler = new GetArtistByIdQueryHandler(artistRepository);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ArtistMissing()
    {
        var act = () => _handler.Handle(new GetArtistByIdQuery(Guid.NewGuid(), "en"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ArtistSoftDeleted()
    {
        var artist = TestData.Person(isDeleted: true);
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(new GetArtistByIdQuery(artist.Id, "en"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnDetail_When_ArtistExists()
    {
        var artist = TestData.Person(name: "Test Artist");
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        var dto = await _handler.Handle(new GetArtistByIdQuery(artist.Id, "en"), CancellationToken.None);

        dto.Id.Should().Be(artist.Id);
        dto.Name.Should().Be("Test Artist");
        dto.Slug.Should().Be(artist.Slug);
        dto.Roles.Should().BeEmpty();
        dto.Filmography.ByRole.Should().BeEmpty();
    }

    public void Dispose() => _dbContext.Dispose();
}
