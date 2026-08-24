using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.ForgotPassword;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _emailSenderMock = new Mock<IEmailSender>();

        _handler = new ForgotPasswordCommandHandler(
            _userManagerMock.Object,
            _emailSenderMock.Object,
            NullLogger<ForgotPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_BeSilent_When_UserNotFound()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        await _handler.Handle(NewCommand("nobody@x.com"), CancellationToken.None);

        _userManagerMock.Verify(m => m.GeneratePasswordResetTokenAsync(It.IsAny<User>()), Times.Never);
        _emailSenderMock.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_BeSilent_When_EmailUnconfirmed()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "u",
            Email = "u@x.com",
            EmailConfirmed = false,
        };
        _userManagerMock.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);

        await _handler.Handle(NewCommand("u@x.com"), CancellationToken.None);

        _userManagerMock.Verify(m => m.GeneratePasswordResetTokenAsync(It.IsAny<User>()), Times.Never);
        _emailSenderMock.Verify(e => e.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_SendResetEmail_When_UserExistsAndConfirmed()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "u",
            Email = "u@x.com",
            PreferredLanguage = "uk",
            EmailConfirmed = true,
        };
        _userManagerMock.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-tok");

        await _handler.Handle(NewCommand("u@x.com"), CancellationToken.None);

        _emailSenderMock.Verify(e => e.SendPasswordResetAsync(
            "u@x.com", "u", user.Id, "reset-tok", "uk", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ForgotPasswordCommand NewCommand(string email)
        => new(new ForgotPasswordRequestDto { Email = email });
}
