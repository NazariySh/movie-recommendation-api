using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Users.Commands.DeleteUser;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Users;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _handler = new DeleteUserCommandHandler(
            _userManagerMock.Object,
            _refreshTokenRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new DeleteUserCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_NoOp_When_AlreadyDeleted()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", IsDeleted = true };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        await _handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Anonymize_RevokeTokens_AndSave()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alice",
            Email = "alice@example.com",
            Bio = "hi",
            AvatarUrl = "avatar.png",
            EmailConfirmed = true,
        };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        user.IsDeleted.Should().BeTrue();
        user.UserName.Should().StartWith(UserAnonymization.UsernamePrefix);
        user.Email.Should().EndWith(UserAnonymization.EmailDomain);
        user.Bio.Should().BeNull();
        user.AvatarUrl.Should().BeNull();
        user.EmailConfirmed.Should().BeFalse();
        user.LockoutEnd.Should().Be(DateTimeOffset.MaxValue);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UpdateFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));

        var act = () => _handler.Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*fail*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
