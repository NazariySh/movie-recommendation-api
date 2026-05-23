using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Commands.ImportArtistFromImdb;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Artists;

public class ImportArtistFromImdbCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ITmdbPersonImporter> _tmdbImporterMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly ImportArtistFromImdbCommandHandler _handler;

    public ImportArtistFromImdbCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _tmdbImporterMock = new Mock<ITmdbPersonImporter>();
        _blobStorageMock = new Mock<IBlobStorageService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var artistRepository = new ArtistRepository(_dbContext, mapperConfig);
        var slugGenerator = new ArtistSlugGenerator(artistRepository);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new ImportArtistFromImdbCommandHandler(
            artistRepository,
            slugGenerator,
            _tmdbImporterMock.Object,
            _blobStorageMock.Object,
            unitOfWork,
            NullLogger<ImportArtistFromImdbCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_ArtistWithSameImdbIdExists()
    {
        _dbContext.People.Add(TestData.Person(imdbId: "nm0001234"));
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(NewCommand("nm0001234"), CancellationToken.None);

        await act.Should().ThrowAsync<AlreadyExistsException>();
        _tmdbImporterMock.Verify(
            t => t.FetchByImdbIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_TmdbReturnsNull()
    {
        _tmdbImporterMock
            .Setup(t => t.FetchByImdbIdAsync("nm0001234", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TmdbPersonResult?)null);

        var act = () => _handler.Handle(NewCommand("nm0001234"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_PersistArtistAndMirrorPhoto_OnSuccess()
    {
        _tmdbImporterMock
            .Setup(t => t.FetchByImdbIdAsync("nm0001234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewTmdbResult("Christopher Nolan", "https://image.tmdb.org/foo.jpg"));
        _blobStorageMock
            .Setup(b => b.CopyFromUrlAsync(
                "https://image.tmdb.org/foo.jpg",
                It.Is<string>(s => s.StartsWith("artists/christopher-nolan")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://blob/artists/christopher-nolan.jpg");

        var id = await _handler.Handle(NewCommand("nm0001234"), CancellationToken.None);

        var saved = await _dbContext.People.SingleAsync(p => p.Id == id);
        saved.Name.Should().Be("Christopher Nolan");
        saved.Slug.Should().Be("christopher-nolan");
        saved.ImdbId.Should().Be("nm0001234");
        saved.PhotoUrl.Should().Be("https://blob/artists/christopher-nolan.jpg");
    }

    [Fact]
    public async Task Handle_Should_FallBackToSourceUrl_When_BlobMirrorFails()
    {
        _tmdbImporterMock
            .Setup(t => t.FetchByImdbIdAsync("nm0001234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewTmdbResult("Christopher Nolan", "https://image.tmdb.org/foo.jpg"));
        _blobStorageMock
            .Setup(b => b.CopyFromUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("blob unavailable"));

        var id = await _handler.Handle(NewCommand("nm0001234"), CancellationToken.None);

        var saved = await _dbContext.People.SingleAsync(p => p.Id == id);
        saved.PhotoUrl.Should().Be("https://image.tmdb.org/foo.jpg");
    }

    [Fact]
    public async Task Handle_Should_HandleTmdbResultWithNoPhoto()
    {
        _tmdbImporterMock
            .Setup(t => t.FetchByImdbIdAsync("nm0001234", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewTmdbResult("Christopher Nolan", profileImageUrl: null));

        var id = await _handler.Handle(NewCommand("nm0001234"), CancellationToken.None);

        var saved = await _dbContext.People.SingleAsync(p => p.Id == id);
        saved.PhotoUrl.Should().BeNull();
        _blobStorageMock.Verify(
            b => b.CopyFromUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ImportArtistFromImdbCommand NewCommand(string imdbId)
        => new(new ImportArtistFromImdbDto { ImdbId = imdbId });

    private static TmdbPersonResult NewTmdbResult(string name, string? profileImageUrl) => new()
    {
        TmdbId = 525,
        ImdbId = "nm0001234",
        Name = name,
        Biography = "A renowned filmmaker.",
        KnownForDepartment = "directing",
        Gender = "Male",
        ProfileImageUrl = profileImageUrl,
    };

    public void Dispose() => _dbContext.Dispose();
}
