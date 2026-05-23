using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.Login;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly Mock<ICookieService> _cookieServiceMock;
    private readonly IMapper _mapper;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userManagerMock = UserManagerMockFactory.Create();
        _tokenProviderMock = new Mock<ITokenProvider>();
        _cookieServiceMock = new Mock<ICookieService>();
        _mapper = TestMapperFactory.Create();

        _handler = new LoginCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userManagerMock.Object,
            _tokenProviderMock.Object,
            _cookieServiceMock.Object,
            _mapper,
            NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UserNotFound()
    {
        _userManagerMock
            .Setup(m => m.FindByEmailAsync("ghost@x.com"))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(NewCommand("ghost@x.com"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UserSoftDeleted()
    {
        _userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", IsDeleted = true });

        var act = () => _handler.Handle(NewCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials*");
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_LockedOut()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var act = () => _handler.Handle(NewCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*locked*");
    }

    [Fact]
    public async Task Handle_Should_ThrowAndIncrementFailures_When_WrongPassword()
    {
        var user = new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var act = () => _handler.Handle(NewCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Invalid credentials*");
        _userManagerMock.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_IssueTokensAndSetCookie_OnSuccess()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "u",
            Email = "u@x.com",
            PreferredLanguage = "en",
        };
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "pwd")).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _tokenProviderMock.Setup(t => t.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>())).Returns("access.jwt");

        var expiry = DateTime.UtcNow.AddDays(7);
        _tokenProviderMock.Setup(t => t.GenerateRefreshToken()).Returns(new RefreshTokenDto("refresh-tok", expiry));

        var result = await _handler.Handle(NewCommand(password: "pwd"), CancellationToken.None);

        result.AccessToken.Should().Be("access.jwt");
        result.User.Id.Should().Be(userId);
        result.User.Username.Should().Be("u");
        result.User.Email.Should().Be("u@x.com");
        result.User.Roles.Should().ContainSingle().Which.Should().Be("User");
        _refreshTokenRepositoryMock.Verify(r => r.Add(It.Is<RefreshToken>(rt => rt.UserId == userId && rt.Token == "refresh-tok")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cookieServiceMock.Verify(c => c.SetCookie(
            RefreshTokenCommandHandler.RefreshTokenCookieName, "refresh-tok", expiry), Times.Once);
        _userManagerMock.Verify(m => m.AccessFailedAsync(It.IsAny<User>()), Times.Never);
    }

    private static LoginCommand NewCommand(string email = "u@x.com", string password = "pwd")
        => new(new LoginRequestDto { Email = email, Password = password, RememberMe = true });
}
