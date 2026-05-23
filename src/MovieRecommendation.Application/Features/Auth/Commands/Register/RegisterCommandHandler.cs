using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : ICommandHandler<RegisterCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        UserManager<User> userManager,
        IEmailSender emailSender,
        ILogger<RegisterCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Unit> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (!await _userRepository.IsEmailUniqueAsync(request.Model.Email, cancellationToken))
        {
            throw new AlreadyExistsException($"User with email {request.Model.Email} already exists");
        }

        if (!await _userRepository.IsUsernameUniqueAsync(request.Model.Username, cancellationToken))
        {
            throw new AlreadyExistsException($"Username '{request.Model.Username}' is already taken");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = new User
            {
                UserName = request.Model.Username,
                Email = request.Model.Email,
                PreferredLanguage = request.Model.PreferredLanguage,
                EmailConfirmed = false,
            };

            var result = await _userManager.CreateAsync(user, request.Model.Password);
            result.EnsureSucceeded("Failed to create user");

            var roleResult = await _userManager.AddToRoleAsync(user, request.RoleType.ToString());
            roleResult.EnsureSucceeded($"Failed to add role '{request.RoleType}' to user");

            var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            await _emailSender.SendVerificationAsync(
                user.Email,
                user.UserName,
                user.Id,
                verificationToken,
                user.PreferredLanguage,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Registered user {UserId} ({Username})", user.Id, user.UserName);

            return Unit.Value;

        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
