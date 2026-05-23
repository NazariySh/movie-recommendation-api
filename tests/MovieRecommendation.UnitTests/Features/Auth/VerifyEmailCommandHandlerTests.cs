using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.VerifyEmail;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _emailSenderMock = new Mock<IEmailSender>();

        _handler = new VerifyEmailCommandHandler(
            _userManagerMock.Object,
            _emailSenderMock.Object,
            NullLogger<VerifyEmailCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(NewCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_NoOp_When_AlreadyConfirmed()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", EmailConfirmed = true };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        await _handler.Handle(NewCommand(user.Id), CancellationToken.None);

        _userManagerMock.Verify(m => m.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _emailSenderMock.Verify(e => e.SendWelcomeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_ConfirmEmailFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", EmailConfirmed = false };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "tok"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var act = () => _handler.Handle(NewCommand(user.Id, "tok"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Invalid token*");
        _emailSenderMock.Verify(e => e.SendWelcomeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ConfirmAndSendWelcome_OnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", EmailConfirmed = false, PreferredLanguage = "uk" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "tok")).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(NewCommand(user.Id, "tok"), CancellationToken.None);

        _emailSenderMock.Verify(e => e.SendWelcomeAsync("u@x.com", "u", "uk", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static VerifyEmailCommand NewCommand(Guid userId, string token = "verify-tok")
        => new(new VerifyEmailRequestDto { UserId = userId, Token = token });
}
