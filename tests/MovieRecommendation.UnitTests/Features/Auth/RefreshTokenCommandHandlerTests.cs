using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private const string CookieName = RefreshTokenCommandHandler.RefreshTokenCookieName;

    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly Mock<ICookieService> _cookieServiceMock;
    private readonly IMapper _mapper;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userManagerMock = UserManagerMockFactory.Create();
        _tokenProviderMock = new Mock<ITokenProvider>();
        _cookieServiceMock = new Mock<ICookieService>();
        _mapper = TestMapperFactory.Create();

        _handler = new RefreshTokenCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userManagerMock.Object,
            _tokenProviderMock.Object,
            _cookieServiceMock.Object,
            _mapper,
            NullLogger<RefreshTokenCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_NoCookie()
    {
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns((string?)null);

        var act = () => _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*required*");
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_TokenNotFound()
    {
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("bogus");
        _refreshTokenRepositoryMock
            .Setup(r => r.FindByTokenAsync("bogus", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var act = () => _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*Invalid*");
    }

    [Fact]
    public async Task Handle_Should_RevokeAllAndThrow_When_TokenAlreadyRevoked()
    {
        var userId = Guid.NewGuid();
        var revoked = new RefreshToken
        {
            UserId = userId,
            Token = "tok",
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(revoked);

        var act = () => _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*re-use*");
        _refreshTokenRepositoryMock.Verify(r => r.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _cookieServiceMock.Verify(c => c.RemoveCookie(CookieName), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_RemoveCookieAndThrow_When_TokenExpired()
    {
        var expired = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "tok",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        };
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(expired);

        var act = () => _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*expired*");
        _cookieServiceMock.Verify(c => c.RemoveCookie(CookieName), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowEmailNotVerified_When_EmailUnconfirmed()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken { UserId = userId, Token = "tok", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "u", Email = "u@x.com", EmailConfirmed = false });

        var act = () => _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EmailNotVerifiedException>();
        _tokenProviderMock.Verify(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_UserIsSoftDeleted()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken { UserId = userId, Token = "tok", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(new User { Id = userId, UserName = "u", Email = "u@x.com", IsDeleted = true });

        var act = () => _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*disabled*");
    }

    [Fact]
    public async Task Handle_Should_RotateTokenAndReturnNewAccessToken_OnSuccess()
    {
        var userId = Guid.NewGuid();
        var stored = new RefreshToken
        {
            UserId = userId,
            Token = "old-tok",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        var user = new User
        {
            Id = userId,
            UserName = "u",
            Email = "u@x.com",
            PreferredLanguage = "en",
            EmailConfirmed = true,
        };
        var newExpiry = DateTime.UtcNow.AddDays(7);

        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("old-tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("old-tok", It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _tokenProviderMock.Setup(t => t.GenerateRefreshToken()).Returns(new RefreshTokenDto("new-tok", newExpiry));
        _tokenProviderMock.Setup(t => t.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>())).Returns("new.jwt");

        var result = await _handler.Handle(new RefreshTokenCommand(), CancellationToken.None);

        result.AccessToken.Should().Be("new.jwt");
        result.User.Id.Should().Be(userId);
        result.User.Username.Should().Be("u");
        result.User.Roles.Should().ContainSingle().Which.Should().Be("User");
        stored.RevokedAt.Should().NotBeNull();
        stored.ReplacedByToken.Should().Be("new-tok");
        _refreshTokenRepositoryMock.Verify(r => r.Add(It.Is<RefreshToken>(t => t.Token == "new-tok" && t.UserId == userId)), Times.Once);
        _cookieServiceMock.Verify(c => c.SetCookie(CookieName, "new-tok", newExpiry), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
