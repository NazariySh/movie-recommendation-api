using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.Register;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _userManagerMock = UserManagerMockFactory.Create();
        _emailSenderMock = new Mock<IEmailSender>();
        _transactionMock = new Mock<IDbContextTransaction>();

        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _handler = new RegisterCommandHandler(
            _unitOfWorkMock.Object,
            _userRepositoryMock.Object,
            _userManagerMock.Object,
            _emailSenderMock.Object,
            NullLogger<RegisterCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_EmailAlreadyTaken()
    {
        _userRepositoryMock
            .Setup(r => r.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(new RegisterCommand(NewDto()), CancellationToken.None);

        await act.Should().ThrowAsync<AlreadyExistsException>().WithMessage("*email*");
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_UsernameAlreadyTaken()
    {
        _userRepositoryMock
            .Setup(r => r.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock
            .Setup(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(new RegisterCommand(NewDto()), CancellationToken.None);

        await act.Should().ThrowAsync<AlreadyExistsException>().WithMessage("*Username*");
        _userManagerMock.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_CreateUserAddRoleAndSendVerification_OnSuccess()
    {
        _userRepositoryMock
            .Setup(r => r.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock
            .Setup(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("verify-token");

        await _handler.Handle(new RegisterCommand(NewDto()), CancellationToken.None);

        _userManagerMock.Verify(m => m.CreateAsync(
            It.Is<User>(u => u.UserName == "newbie" && u.Email == "new@example.com" && !u.EmailConfirmed),
            "Str0ng!Pwd"), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), nameof(RoleType.User)), Times.Once);
        _emailSenderMock.Verify(e => e.SendVerificationAsync(
            "new@example.com", "newbie", It.IsAny<Guid>(), "verify-token", "en", It.IsAny<CancellationToken>()),
            Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_AddProvidedRole_When_AdminProvidesRoleType()
    {
        _userRepositoryMock
            .Setup(r => r.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock
            .Setup(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock
            .Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("t");

        await _handler.Handle(new RegisterCommand(NewDto(), RoleType.Moderator), CancellationToken.None);

        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), nameof(RoleType.Moderator)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_CreateUserFails()
    {
        _userRepositoryMock.Setup(r => r.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userRepositoryMock.Setup(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var act = () => _handler.Handle(new RegisterCommand(NewDto()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Password too weak*");
    }

    [Fact]
    public async Task Handle_Should_CommitTransactionAndSwallowEmailError_When_VerificationEmailFails()
    {
        _userRepositoryMock.Setup(r => r.IsEmailUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userRepositoryMock.Setup(r => r.IsUsernameUniqueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<User>())).ReturnsAsync("verify-token");
        _emailSenderMock
            .Setup(e => e.SendVerificationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        await _handler.Handle(new RegisterCommand(NewDto()), CancellationToken.None);

        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RegisterRequestDto NewDto() => new()
    {
        Username = "newbie",
        Email = "new@example.com",
        Password = "Str0ng!Pwd",
        PreferredLanguage = "en",
    };
}
