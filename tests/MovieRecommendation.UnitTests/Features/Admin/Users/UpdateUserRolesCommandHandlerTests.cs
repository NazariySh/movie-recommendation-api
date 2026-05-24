using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Features.Admin.Users.Commands.UpdateUserRoles;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class UpdateUserRolesCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly UpdateUserRolesCommandHandler _handler;

    public UpdateUserRolesCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _handler = new UpdateUserRolesCommandHandler(
            _userManagerMock.Object,
            _refreshTokenRepositoryMock.Object,
            NullLogger<UpdateUserRolesCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_ActorTargetsSelf()
    {
        var sameId = Guid.NewGuid();

        var act = () => _handler.Handle(
            new UpdateUserRolesCommand(sameId, sameId, new[] { nameof(RoleType.Admin) }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*own roles*");
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new UpdateUserRolesCommand(Guid.NewGuid(), userId, new[] { nameof(RoleType.User) }), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowArgument_When_RolesUnknown()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var act = () => _handler.Handle(new UpdateUserRolesCommand(Guid.NewGuid(), user.Id, new[] { "Superuser" }), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Superuser*");
        _userManagerMock.Verify(m => m.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _userManagerMock.Verify(m => m.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_AddAndRemoveRoles_AndRevokeTokens()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { nameof(RoleType.User) });
        _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains(nameof(RoleType.User)))))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains(nameof(RoleType.Admin)))))
            .ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(new UpdateUserRolesCommand(Guid.NewGuid(), user.Id, new[] { nameof(RoleType.Admin) }), CancellationToken.None);

        _userManagerMock.Verify(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _userManagerMock.Verify(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_NotCallAddOrRemoveOrRevoke_When_RolesUnchanged()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { nameof(RoleType.User), nameof(RoleType.Moderator) });

        await _handler.Handle(
            new UpdateUserRolesCommand(Guid.NewGuid(), user.Id, new[] { nameof(RoleType.User), nameof(RoleType.Moderator) }),
            CancellationToken.None);

        _userManagerMock.Verify(m => m.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _userManagerMock.Verify(m => m.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_RemoveRolesFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(RoleType.Admin) });
        _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "nope" }));

        var act = () => _handler.Handle(
            new UpdateUserRolesCommand(Guid.NewGuid(), user.Id, new[] { nameof(RoleType.User) }),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nope*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
