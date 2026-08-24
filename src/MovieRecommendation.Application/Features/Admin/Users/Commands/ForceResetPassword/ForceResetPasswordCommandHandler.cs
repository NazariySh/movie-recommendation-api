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
    private readonly ILogger<ForceResetPasswordCommandHandler> _logger;

    public ForceResetPasswordCommandHandler(
        UserManager<User> userManager,
        IEmailSender emailSender,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<ForceResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForceResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorUserId == request.UserId)
        {
            throw new ForbiddenException("Admins cannot force-reset their own password. Use the regular change-password flow instead.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User {request.UserId} not found.");

        if (string.IsNullOrEmpty(user.Email))
        {
            throw new ArgumentException("User has no email address; cannot send reset link.", nameof(request.UserId));
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

        _logger.LogInformation("Actor {ActorUserId} triggered password reset for {UserId}", request.ActorUserId, user.Id);

        return Unit.Value;
    }
}
