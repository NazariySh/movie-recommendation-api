using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.ResetPassword;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _handler = new ResetPasswordCommandHandler(
            _userManagerMock.Object,
            _refreshTokenRepositoryMock.Object,
            NullLogger<ResetPasswordCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(NewCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid reset request*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_ResetFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ResetPasswordAsync(user, "tok", "newPwd"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        var act = () => _handler.Handle(NewCommand(user.Id, "tok", "newPwd"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid reset request*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_RevokeAllRefreshTokens_OnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ResetPasswordAsync(user, "tok", "newPwd")).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(NewCommand(user.Id, "tok", "newPwd"), CancellationToken.None);

        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ResetPasswordCommand NewCommand(Guid userId, string token = "tok", string newPassword = "newPwd")
        => new(new ResetPasswordRequestDto { UserId = userId, Token = token, NewPassword = newPassword });
}
