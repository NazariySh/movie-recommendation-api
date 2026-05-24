using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.ForceResetPassword;

public class ForceResetPasswordCommandHandler : ICommandHandler<ForceResetPasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ForceResetPasswordCommandHandler> _logger;

    public ForceResetPasswordCommandHandler(
        UserManager<User> userManager,
        IEmailSender emailSender,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<ForceResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForceResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User {request.UserId} not found.");

        if (string.IsNullOrEmpty(user.Email))
        {
            throw new ForbiddenException("User has no email address; cannot send reset link.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        await _emailSender.SendPasswordResetAsync(
            user.Email,
            user.UserName ?? user.Email,
            user.Id,
            token,
            user.PreferredLanguage,
            cancellationToken);

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin-triggered password reset for {UserId}", user.Id);

        return Unit.Value;
    }
}
