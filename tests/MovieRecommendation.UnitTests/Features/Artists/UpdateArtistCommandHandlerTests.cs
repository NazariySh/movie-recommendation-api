using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Commands.UpdateArtist;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class UpdateArtistCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UpdateArtistCommandHandler _handler;

    public UpdateArtistCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        var slugGenerator = new ArtistSlugGenerator(artistRepository);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new UpdateArtistCommandHandler(artistRepository, slugGenerator, unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_ArtistDoesNotExist()
    {
        var act = () => _handler.Handle(
            new UpdateArtistCommand(Guid.NewGuid(), NewDto()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateFieldsAndKeepSlug_When_NameUnchanged()
    {
        var artist = TestData.Person(name: "Original Name", slug: "original-name");
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        var dto = NewDto(name: "Original Name");
        dto.Biography = "Updated bio";
        dto.Nationality = "French";

        await _handler.Handle(new UpdateArtistCommand(artist.Id, dto), CancellationToken.None);

        var reloaded = await _dbContext.People.SingleAsync(p => p.Id == artist.Id);
        reloaded.Slug.Should().Be("original-name");
        reloaded.Biography.Should().Be("Updated bio");
        reloaded.Nationality.Should().Be("French");
        reloaded.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_RegenerateSlug_When_NameChanged()
    {
        var artist = TestData.Person(name: "Original Name", slug: "original-name");
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateArtistCommand(artist.Id, NewDto(name: "Renamed Artist")),
            CancellationToken.None);

        var reloaded = await _dbContext.People.SingleAsync(p => p.Id == artist.Id);
        reloaded.Name.Should().Be("Renamed Artist");
        reloaded.Slug.Should().Be("renamed-artist");
    }

    [Fact]
    public async Task Handle_Should_AppendSuffix_When_RegeneratedSlugCollidesWithOtherArtist()
    {
        var taken = TestData.Person(name: "Quentin Tarantino", slug: "quentin-tarantino");
        var subject = TestData.Person(name: "Original Name", slug: "original-name");
        _dbContext.People.Add(taken);
        _dbContext.People.Add(subject);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateArtistCommand(subject.Id, NewDto(name: "Quentin Tarantino")),
            CancellationToken.None);

        var reloaded = await _dbContext.People.SingleAsync(p => p.Id == subject.Id);
        reloaded.Slug.Should().Be("quentin-tarantino-2");
    }

    [Fact]
    public async Task Handle_Should_KeepCurrentSlug_When_NameMatchesOwnSlug()
    {
        var artist = TestData.Person(name: "Original", slug: "original");
        _dbContext.People.Add(artist);
        await _dbContext.SaveChangesAsync();

        await _handler.Handle(
            new UpdateArtistCommand(artist.Id, NewDto(name: "Original")),
            CancellationToken.None);

        var reloaded = await _dbContext.People.SingleAsync(p => p.Id == artist.Id);
        reloaded.Slug.Should().Be("original");
    }

    private static UpdateArtistDto NewDto(string name = "Test Artist") => new()
    {
        Name = name,
        Biography = "Bio",
        KnownForDepartment = "acting",
    };

    public void Dispose() => _dbContext.Dispose();
}
