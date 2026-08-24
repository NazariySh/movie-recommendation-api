using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.Features.Auth.Commands.Logout;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class LogoutCommandHandlerTests
{
    private const string CookieName = RefreshTokenCommandHandler.RefreshTokenCookieName;

    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICookieService> _cookieServiceMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cookieServiceMock = new Mock<ICookieService>();

        _handler = new LogoutCommandHandler(
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _cookieServiceMock.Object,
            NullLogger<LogoutCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_RemoveCookie_When_NoTokenInCookie()
    {
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns((string?)null);

        await _handler.Handle(new LogoutCommand(Guid.NewGuid()), CancellationToken.None);

        _cookieServiceMock.Verify(c => c.RemoveCookie(CookieName), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.Update(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_RevokeStoredTokenAndRemoveCookie_When_TokenExists()
    {
        var stored = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "tok",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        await _handler.Handle(new LogoutCommand(stored.UserId), CancellationToken.None);

        stored.RevokedAt.Should().NotBeNull();
        _refreshTokenRepositoryMock.Verify(r => r.Update(stored), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cookieServiceMock.Verify(c => c.RemoveCookie(CookieName), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_NotDoubleRevoke_When_TokenAlreadyRevoked()
    {
        var stored = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            Token = "tok",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
        };
        _cookieServiceMock.Setup(c => c.GetCookie(CookieName)).Returns("tok");
        _refreshTokenRepositoryMock.Setup(r => r.FindByTokenAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        await _handler.Handle(new LogoutCommand(stored.UserId), CancellationToken.None);

        _refreshTokenRepositoryMock.Verify(r => r.Update(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cookieServiceMock.Verify(c => c.RemoveCookie(CookieName), Times.Once);
    }
}
