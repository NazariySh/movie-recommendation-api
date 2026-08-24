using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.GoogleAuth;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Models;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class GoogleAuthCommandHandlerTests
{
    private readonly Mock<IGoogleAuthValidator> _googleValidatorMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenProvider> _tokenProviderMock;
    private readonly Mock<ICookieService> _cookieServiceMock;
    private readonly IMapper _mapper;
    private readonly GoogleAuthCommandHandler _handler;

    public GoogleAuthCommandHandlerTests()
    {
        _googleValidatorMock = new Mock<IGoogleAuthValidator>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userManagerMock = UserManagerMockFactory.Create();
        _tokenProviderMock = new Mock<ITokenProvider>();
        _cookieServiceMock = new Mock<ICookieService>();
        _mapper = TestMapperFactory.Create();

        var transactionMock = new Mock<IDbContextTransaction>();
        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _handler = new GoogleAuthCommandHandler(
            _googleValidatorMock.Object,
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userManagerMock.Object,
            _tokenProviderMock.Object,
            _cookieServiceMock.Object,
            _mapper,
            NullLogger<GoogleAuthCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_GoogleEmailNotVerified()
    {
        _googleValidatorMock
            .Setup(g => g.ValidateAsync("bad-tok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GooglePayload { Email = "g@x.com", EmailVerified = false });

        var act = () => _handler.Handle(NewCommand("bad-tok"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*not verified*");
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorized_When_ExistingUserSoftDeleted()
    {
        _googleValidatorMock
            .Setup(g => g.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GooglePayload { Email = "u@x.com", EmailVerified = true });
        _userManagerMock
            .Setup(m => m.FindByEmailAsync("u@x.com"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), UserName = "u", Email = "u@x.com", IsDeleted = true });

        var act = () => _handler.Handle(NewCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*disabled*");
    }

    [Fact]
    public async Task Handle_Should_CreateUserAndReturnLoginResponse_When_NoExistingUser()
    {
        _googleValidatorMock
            .Setup(g => g.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GooglePayload { Email = "new@x.com", EmailVerified = true, Picture = "avatar" });
        _userManagerMock
            .Setup(m => m.FindByEmailAsync("new@x.com"))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), nameof(RoleType.User))).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string> { "User" });
        _tokenProviderMock.Setup(t => t.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).Returns("g.jwt");
        var expiry = DateTime.UtcNow.AddDays(7);
        _tokenProviderMock.Setup(t => t.GenerateRefreshToken()).Returns(new RefreshTokenDto("g-refresh", expiry));

        var result = await _handler.Handle(NewCommand(), CancellationToken.None);

        result.AccessToken.Should().Be("g.jwt");
        result.User.Email.Should().Be("new@x.com");
        result.User.Roles.Should().ContainSingle().Which.Should().Be("User");
        _userManagerMock.Verify(m => m.CreateAsync(It.Is<User>(u =>
            u.Email == "new@x.com" && u.EmailConfirmed && u.AvatarUrl == "avatar")), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), nameof(RoleType.User)), Times.Once);
        _refreshTokenRepositoryMock.Verify(r => r.Add(It.Is<RefreshToken>(rt => rt.Token == "g-refresh")), Times.Once);
        _cookieServiceMock.Verify(c => c.SetCookie(
            RefreshTokenCommandHandler.RefreshTokenCookieName, "g-refresh", expiry), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReuseExistingUser_When_AlreadyRegistered()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "u",
            Email = "u@x.com",
            PreferredLanguage = "en",
        };
        _googleValidatorMock
            .Setup(g => g.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GooglePayload { Email = "u@x.com", EmailVerified = true });
        _userManagerMock.Setup(m => m.FindByEmailAsync("u@x.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
        _tokenProviderMock.Setup(t => t.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>())).Returns("g.jwt");
        _tokenProviderMock.Setup(t => t.GenerateRefreshToken()).Returns(new RefreshTokenDto("g-tok", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(NewCommand(), CancellationToken.None);

        result.AccessToken.Should().Be("g.jwt");
        result.User.Username.Should().Be("u");
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    private static GoogleAuthCommand NewCommand(string idToken = "id-tok")
        => new(new GoogleAuthRequestDto { IdToken = idToken });
}
