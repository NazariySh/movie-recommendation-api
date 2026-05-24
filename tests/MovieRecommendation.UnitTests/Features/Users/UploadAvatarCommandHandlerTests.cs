using System.Net;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Application.Features.Users.Commands.UploadAvatar;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Users;

public class UploadAvatarCommandHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly IMapper _mapper;
    private readonly UploadAvatarCommandHandler _handler;

    public UploadAvatarCommandHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _blobStorageMock = new Mock<IBlobStorageService>();
        _mapper = TestMapperFactory.Create();

        _handler = new UploadAvatarCommandHandler(
            _userManagerMock.Object,
            _blobStorageMock.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_BlobStorageNotConfigured()
    {
        _blobStorageMock.SetupGet(b => b.IsConfigured).Returns(false);

        var act = () => _handler.Handle(NewCommand(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _userManagerMock.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _blobStorageMock.SetupGet(b => b.IsConfigured).Returns(true);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(NewCommand(userId: userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowUnsupportedMediaType_When_ContentTypeNotAllowed()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _blobStorageMock.SetupGet(b => b.IsConfigured).Returns(true);
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var act = () => _handler.Handle(NewCommand(userId: user.Id, contentType: "image/bmp"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        _blobStorageMock.Verify(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_UploadDeleteOtherExtensions_AndPersistAvatarUrl()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _blobStorageMock.SetupGet(b => b.IsConfigured).Returns(true);
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _blobStorageMock
            .Setup(b => b.UploadAsync($"avatars/{user.Id}.png", It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync($"https://blob/avatars/{user.Id}.png");

        var dto = await _handler.Handle(NewCommand(userId: user.Id, contentType: "image/png"), CancellationToken.None);

        dto.AvatarUrl.Should().Be($"https://blob/avatars/{user.Id}.png");
        user.AvatarUrl.Should().Be($"https://blob/avatars/{user.Id}.png");
        _blobStorageMock.Verify(b => b.DeleteAsync($"avatars/{user.Id}.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _blobStorageMock.Verify(b => b.DeleteAsync($"avatars/{user.Id}.webp", It.IsAny<CancellationToken>()), Times.Once);
        _blobStorageMock.Verify(b => b.DeleteAsync($"avatars/{user.Id}.png", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UserUpdateFails()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _blobStorageMock.SetupGet(b => b.IsConfigured).Returns(true);
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _blobStorageMock
            .Setup(b => b.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://blob/avatars/x.jpg");
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "boom" }));

        var act = () => _handler.Handle(NewCommand(userId: user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*boom*");
    }

    private static UploadAvatarCommand NewCommand(
        Guid? userId = null,
        string contentType = "image/jpeg",
        long length = 1024)
    {
        var stream = new MemoryStream([0x01, 0x02, 0x03]);
        return new UploadAvatarCommand(userId ?? Guid.NewGuid(), stream, contentType, length);
    }
}
