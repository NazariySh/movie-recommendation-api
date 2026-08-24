using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Features.Admin.Users.Commands.DisableUser;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class DisableUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly DisableUserCommandHandler _handler;

    public DisableUserCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new DisableUserCommandHandler(
            _userManagerMock.Object,
            _refreshTokenRepositoryMock.Object,
            _cacheMock.Object,
            NullLogger<DisableUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_ActorTargetsSelf()
    {
        var sameId = Guid.NewGuid();

        var act = () => _handler.Handle(new DisableUserCommand(sameId, sameId, null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*disable themselves*");
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new DisableUserCommand(Guid.NewGuid(), userId, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_MarkDeleted_RevokeTokens_AndInvalidateCaches()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", IsDeleted = false };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(new DisableUserCommand(Guid.NewGuid(), user.Id, "spam"), CancellationToken.None);

        user.IsDeleted.Should().BeTrue();
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(UserCacheKeys.StatsForUser(user.Id)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(user.Id)), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(user.Id)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UpdateFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var act = () => _handler.Handle(new DisableUserCommand(Guid.NewGuid(), user.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*boom*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>()), Times.Never);
    }
}
