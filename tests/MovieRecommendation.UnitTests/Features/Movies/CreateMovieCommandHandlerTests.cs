using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;
using MovieRecommendation.Application.Features.Movies.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class CreateMovieCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly CreateMovieCommandHandler _handler;

    public CreateMovieCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var movieRepository = new MovieRepository(_dbContext, mapperConfig);
        var keyGenerator = new MovieKeyGenerator(movieRepository);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new CreateMovieCommandHandler(
            movieRepository,
            keyGenerator,
            unitOfWork,
            TestMapperFactory.Create(),
            new FakeImageMirror(),
            _cacheMock.Object,
            NullLogger<CreateMovieCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_PersistMovieWithGeneratedKey_And_InvalidateListCache()
    {
        var dto = NewDto(originalTitle: "Inception");

        var id = await _handler.Handle(new CreateMovieCommand(dto), CancellationToken.None);

        var saved = await _dbContext.Movies.SingleAsync(m => m.Id == id);
        saved.OriginalTitle.Should().Be("Inception");
        saved.Key.Should().Be("inception");
        _cacheMock.Verify(c => c.RemoveByPrefix(MovieCacheKeys.ListPrefix), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_AppendSuffix_When_KeyAlreadyExists()
    {
        _dbContext.Movies.Add(TestData.Movie(title: "Inception", key: "inception"));
        await _dbContext.SaveChangesAsync();

        var id = await _handler.Handle(new CreateMovieCommand(NewDto(originalTitle: "Inception")), CancellationToken.None);

        var saved = await _dbContext.Movies.SingleAsync(m => m.Id == id);
        saved.Key.Should().Be("inception-2");
    }

    [Fact]
    public async Task Handle_Should_HonorRequestedKey_When_Provided()
    {
        var dto = NewDto(originalTitle: "Inception");
        dto.Key = "my-custom-key";

        var id = await _handler.Handle(new CreateMovieCommand(dto), CancellationToken.None);

        var saved = await _dbContext.Movies.SingleAsync(m => m.Id == id);
        saved.Key.Should().Be("my-custom-key");
    }

    [Fact]
    public async Task Handle_Should_PersistTranslationsLowercasedLang()
    {
        var dto = NewDto(originalTitle: "Inception");
        dto.Translations =
        [
            new MovieTranslationDto { LanguageCode = "EN", Title = "Inception", Overview = "" },
            new MovieTranslationDto { LanguageCode = "UK", Title = "Початок", Overview = "" },
        ];

        var id = await _handler.Handle(new CreateMovieCommand(dto), CancellationToken.None);

        var saved = await _dbContext.Movies
            .Include(m => m.Translations)
            .SingleAsync(m => m.Id == id);

        saved.Translations.Select(t => t.LanguageCode).Should().BeEquivalentTo(["en", "uk"]);
    }

    private static CreateMovieDto NewDto(string originalTitle = "Test Movie") => new()
    {
        OriginalTitle = originalTitle,
        OriginalLang = "en",
        Type = TitleType.Movie,
    };

    public void Dispose() => _dbContext.Dispose();
}
