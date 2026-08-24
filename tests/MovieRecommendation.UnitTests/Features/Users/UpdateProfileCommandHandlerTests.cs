using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Users.Commands.UpdateProfile;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Users;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly IMapper _mapper;
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _userRepositoryMock = new Mock<IUserRepository>();
        _mapper = TestMapperFactory.Create();
        _handler = new UpdateProfileCommandHandler(_userManagerMock.Object, _userRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(
            new UpdateProfileCommand(userId, new UpdateProfileDto { Username = "x", PreferredLanguage = "uk" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowAlreadyExists_When_UsernameTaken()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.IsUsernameUniqueAsync("bob", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _handler.Handle(
            new UpdateProfileCommand(user.Id, new UpdateProfileDto { Username = "bob", PreferredLanguage = "uk" }),
            CancellationToken.None);

        await act.Should().ThrowAsync<AlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_Should_TrimBio_And_NullifyWhitespace()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        await _handler.Handle(
            new UpdateProfileCommand(user.Id, new UpdateProfileDto { Username = "alice", Bio = "   ", PreferredLanguage = "uk" }),
            CancellationToken.None);

        user.Bio.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnDto_With_UpdatedFields()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alice",
            Email = "alice@example.com",
        };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var dto = await _handler.Handle(
            new UpdateProfileCommand(user.Id, new UpdateProfileDto { Username = "alice", Bio = "new", PreferredLanguage = "en" }),
            CancellationToken.None);

        dto.Bio.Should().Be("new");
        dto.PreferredLanguage.Should().Be("en");
        dto.Roles.Should().BeEquivalentTo(new[] { "User" });
    }

    [Fact]
    public async Task Handle_Should_NotCheckUsernameUniqueness_When_Unchanged()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        await _handler.Handle(
            new UpdateProfileCommand(user.Id, new UpdateProfileDto { Username = "alice", PreferredLanguage = "uk" }),
            CancellationToken.None);

        _userRepositoryMock.Verify(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userManagerMock.Verify(m => m.SetUserNameAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
}
