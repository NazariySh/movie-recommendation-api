using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Features.Admin.Users.Commands.ForceResetPassword;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class ForceResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly ForceResetPasswordCommandHandler _handler;

    public ForceResetPasswordCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _emailSenderMock = new Mock<IEmailSender>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _handler = new ForceResetPasswordCommandHandler(
            _userManagerMock.Object,
            _emailSenderMock.Object,
            _refreshTokenRepositoryMock.Object,
            NullLogger<ForceResetPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_ActorTargetsSelf()
    {
        var sameId = Guid.NewGuid();

        var act = () => _handler.Handle(new ForceResetPasswordCommand(sameId, sameId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*own password*");
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new ForceResetPasswordCommand(Guid.NewGuid(), userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _emailSenderMock.Verify(
            e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgument_When_UserHasNoEmail()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = null };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var act = () => _handler.Handle(new ForceResetPasswordCommand(Guid.NewGuid(), user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _emailSenderMock.Verify(
            e => e.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_SendResetEmail_And_RevokeTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alice",
            Email = "alice@example.com",
            PreferredLanguage = "en",
        };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

        await _handler.Handle(new ForceResetPasswordCommand(Guid.NewGuid(), user.Id), CancellationToken.None);

        _emailSenderMock.Verify(
            e => e.SendPasswordResetAsync(user.Email, user.UserName!, user.Id, "reset-token", "en", It.IsAny<CancellationToken>()),
            Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
