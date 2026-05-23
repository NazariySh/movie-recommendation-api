using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Features.Users.Queries.GetMyGenrePreferences;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.UnitTests.Features.Users;

public class GetMyGenrePreferencesQueryHandlerTests
{
    private readonly Mock<IUserGenrePreferenceRepository> _repositoryMock;
    private readonly GetMyGenrePreferencesQueryHandler _handler;

    public GetMyGenrePreferencesQueryHandlerTests()
    {
        _repositoryMock = new Mock<IUserGenrePreferenceRepository>();
        _handler = new GetMyGenrePreferencesQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_NoPreferences()
    {
        _repositoryMock
            .Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserGenrePreference>());

        var result = await _handler.Handle(new GetMyGenrePreferencesQuery(Guid.NewGuid(), "uk"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_LocaliseGenreName_AndFallBackToEnglish()
    {
        var userId = Guid.NewGuid();
        var genre1 = new Genre
        {
            Id = 1,
            Slug = "action",
            Translations =
            {
                new GenreTranslation { GenreId = 1, LanguageCode = "uk", Name = "Бойовик" },
                new GenreTranslation { GenreId = 1, LanguageCode = "en", Name = "Action" },
            },
        };
        var genre2 = new Genre
        {
            Id = 2,
            Slug = "drama",
            Translations =
            {
                new GenreTranslation { GenreId = 2, LanguageCode = "en", Name = "Drama" },
            },
        };

        var prefs = new List<UserGenrePreference>
        {
            new() { UserId = userId, GenreId = 1, Weight = 0.8m, Genre = genre1 },
            new() { UserId = userId, GenreId = 2, Weight = 0.3m, Genre = genre2 },
        };

        _repositoryMock
            .Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        var result = await _handler.Handle(new GetMyGenrePreferencesQuery(userId, "uk"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Бойовик");
        result[1].Name.Should().Be("Drama");
    }
}
