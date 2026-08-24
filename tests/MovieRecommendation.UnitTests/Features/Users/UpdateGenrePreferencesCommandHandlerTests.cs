using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Users.Commands.UpdateGenrePreferences;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Users;

public class UpdateGenrePreferencesCommandHandlerTests
{
    private readonly Mock<IUserGenrePreferenceRepository> _repositoryMock;
    private readonly Mock<IGenreRepository> _genreRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly UpdateGenrePreferencesCommandHandler _handler;

    public UpdateGenrePreferencesCommandHandlerTests()
    {
        _repositoryMock = new Mock<IUserGenrePreferenceRepository>();
        _genreRepositoryMock = new Mock<IGenreRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new UpdateGenrePreferencesCommandHandler(
            _repositoryMock.Object,
            _genreRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_GenreUnknown()
    {
        _genreRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Genre>());

        var act = () => _handler.Handle(
            new UpdateGenrePreferencesCommand(
                Guid.NewGuid(),
                new[] { new UpdateGenrePreferenceDto { GenreId = 99, Weight = 0.5m } },
                "uk"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_Should_ClampWeight_ToZeroOne()
    {
        var userId = Guid.NewGuid();
        var genre = BuildGenre(1, "action", uk: "Бойовик");
        _genreRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Genre> { [1] = genre });

        IReadOnlyCollection<UserGenrePreference>? captured = null;
        _repositoryMock
            .Setup(r => r.ReplaceForUserAsync(userId, It.IsAny<IReadOnlyCollection<UserGenrePreference>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyCollection<UserGenrePreference>, CancellationToken>((_, rows, _) => captured = rows)
            .Returns(Task.CompletedTask);

        await _handler.Handle(
            new UpdateGenrePreferencesCommand(
                userId,
                new[]
                {
                    new UpdateGenrePreferenceDto { GenreId = 1, Weight = 1.5m },
                },
                "uk"),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Single().Weight.Should().Be(1m);
    }

    [Fact]
    public async Task Handle_Should_LocaliseGenreName_UsingRequestedLang()
    {
        var userId = Guid.NewGuid();
        var genre = BuildGenre(1, "action", uk: "Бойовик", en: "Action");
        _genreRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Genre> { [1] = genre });

        var result = await _handler.Handle(
            new UpdateGenrePreferencesCommand(
                userId,
                new[] { new UpdateGenrePreferenceDto { GenreId = 1, Weight = 1m } },
                "uk"),
            CancellationToken.None);

        result.Single().Name.Should().Be("Бойовик");
    }

    [Fact]
    public async Task Handle_Should_FallBackToEnglish_When_RequestedLangMissing()
    {
        var userId = Guid.NewGuid();
        var genre = BuildGenre(1, "action", en: "Action");
        _genreRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Genre> { [1] = genre });

        var result = await _handler.Handle(
            new UpdateGenrePreferencesCommand(
                userId,
                new[] { new UpdateGenrePreferenceDto { GenreId = 1, Weight = 1m } },
                "uk"),
            CancellationToken.None);

        result.Single().Name.Should().Be("Action");
    }

    [Fact]
    public async Task Handle_Should_InvalidateRecommendationCaches()
    {
        var userId = Guid.NewGuid();
        var genre = BuildGenre(1, "action", en: "Action");
        _genreRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, Genre> { [1] = genre });

        await _handler.Handle(
            new UpdateGenrePreferencesCommand(
                userId,
                new[] { new UpdateGenrePreferenceDto { GenreId = 1, Weight = 1m } },
                "en"),
            CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(userId)), Times.Once);
    }

    private static Genre BuildGenre(int id, string slug, string? uk = null, string? en = null)
    {
        var genre = new Genre { Id = id, Slug = slug };
        if (uk is not null)
        {
            genre.Translations.Add(new GenreTranslation { GenreId = id, LanguageCode = "uk", Name = uk });
        }
        if (en is not null)
        {
            genre.Translations.Add(new GenreTranslation { GenreId = id, LanguageCode = "en", Name = en });
        }
        return genre;
    }
}
