using FluentAssertions;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Queries.GetAllArtists;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class GetAllArtistsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetAllArtistsQueryHandler _handler;

    public GetAllArtistsQueryHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        _handler = new GetAllArtistsQueryHandler(artistRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoArtists()
    {
        var result = await _handler.Handle(new GetAllArtistsQuery(NewSearch()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_ExcludeSoftDeletedArtists()
    {
        _dbContext.People.Add(TestData.Person(name: "Live Artist", slug: "live", isDeleted: false));
        _dbContext.People.Add(TestData.Person(name: "Gone Artist", slug: "gone", isDeleted: true));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllArtistsQuery(NewSearch()), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Name.Should().Be("Live Artist");
    }

    [Fact]
    public async Task Handle_Should_PaginateResults()
    {
        for (var i = 0; i < 5; i++)
        {
            _dbContext.People.Add(TestData.Person(name: $"Artist {i}", slug: $"artist-{i}"));
        }
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetAllArtistsQuery(NewSearch(pageNumber: 1, pageSize: 2)),
            CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(2);
        result.TotalPages.Should().Be(3);
    }

    private static SearchArtistsDto NewSearch(int pageNumber = 1, int pageSize = 10) => new()
    {
        PageNumber = pageNumber,
        PageSize = pageSize,
        SortBy = "name",
    };

    public void Dispose() => _dbContext.Dispose();
}
