using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.ResendVerification;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class ResendVerificationCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly ResendVerificationCommandHandler _handler;

    public ResendVerificationCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _emailSenderMock = new Mock<IEmailSender>();

        _handler = new ResendVerificationCommandHandler(_userManagerMock.Object, _emailSenderMock.Object);
    }

    [Fact]
    public async Task Handle_Should_DoNothing_When_UserNotFound()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        await _handler.Handle(NewCommand("nobody@x.com"), CancellationToken.None);

        _userManagerMock.Verify(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()), Times.Never);
        _emailSenderMock.Verify(e => e.SendVerificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_DoNothing_When_AlreadyConfirmed()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", EmailConfirmed = true };
        _userManagerMock.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        await _handler.Handle(NewCommand("u@x.com"), CancellationToken.None);

        _userManagerMock.Verify(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()), Times.Never);
        _emailSenderMock.Verify(e => e.SendVerificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_SendVerification_When_Unconfirmed()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", EmailConfirmed = false, PreferredLanguage = "uk" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("verify-tok");

        await _handler.Handle(NewCommand("u@x.com"), CancellationToken.None);

        _emailSenderMock.Verify(e => e.SendVerificationAsync(
            "u@x.com", "u", user.Id, "verify-tok", "uk", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ResendVerificationCommand NewCommand(string email)
        => new(new ResendVerificationRequestDto { Email = email });
}
