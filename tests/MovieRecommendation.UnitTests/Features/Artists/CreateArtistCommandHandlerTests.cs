using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Commands.CreateArtist;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class CreateArtistCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CreateArtistCommandHandler _handler;

    public CreateArtistCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        var slugGenerator = new ArtistSlugGenerator(artistRepository);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new CreateArtistCommandHandler(artistRepository, slugGenerator, new FakeImageMirror(), unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_PersistArtistWithGeneratedSlug_When_PayloadIsValid()
    {
        var dto = NewDto(name: "Christopher Nolan");

        var id = await _handler.Handle(new CreateArtistCommand(dto), CancellationToken.None);

        var saved = await _dbContext.People.SingleAsync(p => p.Id == id);
        saved.Name.Should().Be("Christopher Nolan");
        saved.Slug.Should().Be("christopher-nolan");
        saved.Biography.Should().Be(dto.Biography);
        saved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_AppendSuffixToSlug_When_BaseSlugAlreadyExists()
    {
        _dbContext.People.Add(TestData.Person(name: "Christopher Nolan", slug: "christopher-nolan"));
        _dbContext.People.Add(TestData.Person(name: "Christopher Nolan", slug: "christopher-nolan-2"));
        await _dbContext.SaveChangesAsync();

        var id = await _handler.Handle(new CreateArtistCommand(NewDto(name: "Christopher Nolan")), CancellationToken.None);

        var saved = await _dbContext.People.SingleAsync(p => p.Id == id);
        saved.Slug.Should().Be("christopher-nolan-3");
    }

    [Fact]
    public async Task Handle_Should_TreatSoftDeletedSlugAsFree()
    {
        _dbContext.People.Add(TestData.Person(name: "Christopher Nolan", slug: "christopher-nolan", isDeleted: true));
        await _dbContext.SaveChangesAsync();

        var id = await _handler.Handle(new CreateArtistCommand(NewDto(name: "Christopher Nolan")), CancellationToken.None);

        var saved = await _dbContext.People.SingleAsync(p => p.Id == id);
        saved.Slug.Should().Be("christopher-nolan");
    }

    private static CreateArtistDto NewDto(string name = "Test Artist") => new()
    {
        Name = name,
        Biography = "Test biography.",
        KnownForDepartment = "directing",
        Gender = "Male",
        PlaceOfBirth = "London, UK",
        Nationality = "British",
    };

    public void Dispose() => _dbContext.Dispose();
}
