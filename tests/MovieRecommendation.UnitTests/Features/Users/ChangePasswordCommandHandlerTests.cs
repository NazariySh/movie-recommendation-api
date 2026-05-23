using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Users.Commands.ChangePassword;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Users;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new ChangePasswordCommandHandler(
            _userManagerMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(NewCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_PasswordChangeFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ChangePasswordAsync(user, "old", "new"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "wrong password" }));

        var act = () => _handler.Handle(NewCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*wrong password*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_RevokeAllRefreshTokens_OnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ChangePasswordAsync(user, "old", "new")).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(NewCommand(user.Id), CancellationToken.None);

        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ChangePasswordCommand NewCommand(Guid userId) =>
        new(userId, new ChangePasswordRequestDto { CurrentPassword = "old", NewPassword = "new" });
}
