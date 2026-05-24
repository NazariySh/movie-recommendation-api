using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Features.Admin.Users.Commands.EnableUser;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class EnableUserCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly EnableUserCommandHandler _handler;

    public EnableUserCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _handler = new EnableUserCommandHandler(
            _userManagerMock.Object,
            NullLogger<EnableUserCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new EnableUserCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ClearLockout_And_ClearIsDeleted()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "u",
            IsDeleted = true,
            LockoutEnd = DateTimeOffset.MaxValue,
        };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        await _handler.Handle(new EnableUserCommand(user.Id), CancellationToken.None);

        user.IsDeleted.Should().BeFalse();
        _userManagerMock.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UpdateFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));

        var act = () => _handler.Handle(new EnableUserCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*fail*");
    }
}
