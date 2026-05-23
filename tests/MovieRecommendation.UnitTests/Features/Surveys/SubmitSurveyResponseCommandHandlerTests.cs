using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using MovieRecommendation.Application.DTOs.Surveys;
using MovieRecommendation.Application.Features.Surveys.Commands.SubmitSurveyResponse;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Surveys;

public class SubmitSurveyResponseCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ISurveyResponseRepository> _surveyRepoMock;
    private readonly Mock<IGenreRepository> _genreRepoMock;
    private readonly Mock<IUserGenrePreferenceRepository> _preferenceRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IOptionsMonitor<SurveySettings>> _surveyOptionsMock;
    private readonly SubmitSurveyResponseCommandHandler _handler;

    public SubmitSurveyResponseCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _surveyRepoMock = new Mock<ISurveyResponseRepository>();
        _genreRepoMock = new Mock<IGenreRepository>();
        _preferenceRepoMock = new Mock<IUserGenrePreferenceRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cacheMock = new Mock<ICacheService>();
        _surveyOptionsMock = new Mock<IOptionsMonitor<SurveySettings>>();
        _surveyOptionsMock.Setup(m => m.CurrentValue).Returns(new SurveySettings { Version = 1 });

        _handler = new SubmitSurveyResponseCommandHandler(
            _userManagerMock.Object,
            _surveyRepoMock.Object,
            _genreRepoMock.Object,
            _preferenceRepoMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object,
            _surveyOptionsMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(NewCommand(userId, version: 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_SurveyVersionMismatches()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var act = () => _handler.Handle(NewCommand(user.Id, version: 99), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*99*");
    }

    [Fact]
    public async Task Handle_Should_PersistResponse_AndMarkOnboardingComplete()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", OnboardingCompleted = false };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _preferenceRepoMock
            .Setup(r => r.GetGenreIdsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());
        _genreRepoMock
            .Setup(r => r.GetBySlugsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Genre>());

        await _handler.Handle(NewCommand(user.Id, version: 1), CancellationToken.None);

        user.OnboardingCompleted.Should().BeTrue();
        _surveyRepoMock.Verify(r => r.Add(It.Is<SurveyResponse>(s => s.UserId == user.Id && s.Version == 1)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_SkipPreferenceWrite_When_UserAlreadyHasPreferences()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _preferenceRepoMock
            .Setup(r => r.GetGenreIdsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1, 2 });

        await _handler.Handle(NewCommand(user.Id, version: 1), CancellationToken.None);

        _preferenceRepoMock.Verify(
            r => r.ReplaceForUserAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<UserGenrePreference>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _genreRepoMock.Verify(
            r => r.GetBySlugsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_WritePreferencesFromMapper_When_UserHasNone()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _preferenceRepoMock
            .Setup(r => r.GetGenreIdsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var actionGenre = new Genre { Id = 10, Slug = "action" };
        _genreRepoMock
            .Setup(r => r.GetBySlugsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Genre> { ["action"] = actionGenre });

        var answers = JsonDocument.Parse("{\"mood_preferences\":{\"action_intense\":5}}").RootElement;
        await _handler.Handle(
            new SubmitSurveyResponseCommand(user.Id, new SubmitSurveyDto { Version = 1, Answers = answers }),
            CancellationToken.None);

        _preferenceRepoMock.Verify(
            r => r.ReplaceForUserAsync(
                user.Id,
                It.Is<IReadOnlyCollection<UserGenrePreference>>(prefs => prefs.Any(p => p.GenreId == 10)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_InvalidateRecommendationCaches()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _preferenceRepoMock
            .Setup(r => r.GetGenreIdsAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());
        _genreRepoMock
            .Setup(r => r.GetBySlugsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Genre>());

        await _handler.Handle(NewCommand(user.Id, version: 1), CancellationToken.None);

        _cacheMock.Verify(c => c.RemoveByPrefix(It.Is<string>(s => s.Contains(user.Id.ToString()))), Times.AtLeast(2));
    }

    private static SubmitSurveyResponseCommand NewCommand(Guid userId, int version)
    {
        var answers = JsonDocument.Parse("{}").RootElement;
        return new SubmitSurveyResponseCommand(userId, new SubmitSurveyDto { Version = version, Answers = answers });
    }
}
