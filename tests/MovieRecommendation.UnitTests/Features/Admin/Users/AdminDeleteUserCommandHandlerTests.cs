using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Admin.Users.Commands.AdminDeleteUser;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class AdminDeleteUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly AdminDeleteUserCommandHandler _handler;

    public AdminDeleteUserCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new AdminDeleteUserCommandHandler(
            _userManagerMock.Object,
            _refreshTokenRepositoryMock.Object,
            _cacheMock.Object,
            NullLogger<AdminDeleteUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_ActorTargetsSelf()
    {
        var sameId = Guid.NewGuid();

        var act = () => _handler.Handle(new AdminDeleteUserCommand(sameId, sameId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*delete themselves*");
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new AdminDeleteUserCommand(Guid.NewGuid(), userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_NoOp_When_AlreadyDeleted()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", IsDeleted = true };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        await _handler.Handle(new AdminDeleteUserCommand(Guid.NewGuid(), user.Id), CancellationToken.None);

        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Anonymize_RevokeTokens_AndInvalidateCaches()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alice",
            Email = "alice@example.com",
            Bio = "hi",
            AvatarUrl = "avatar.png",
            EmailConfirmed = true,
            IsDeleted = false,
        };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(new AdminDeleteUserCommand(Guid.NewGuid(), user.Id), CancellationToken.None);

        user.IsDeleted.Should().BeTrue();
        user.UserName.Should().StartWith(UserAnonymization.UsernamePrefix);
        user.Email.Should().EndWith(UserAnonymization.EmailDomain);
        user.Bio.Should().BeNull();
        user.AvatarUrl.Should().BeNull();
        user.EmailConfirmed.Should().BeFalse();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(UserCacheKeys.StatsForUser(user.Id)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(user.Id)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(user.Id)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UpdateFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "bad" }));

        var act = () => _handler.Handle(new AdminDeleteUserCommand(Guid.NewGuid(), user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bad*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }
}
