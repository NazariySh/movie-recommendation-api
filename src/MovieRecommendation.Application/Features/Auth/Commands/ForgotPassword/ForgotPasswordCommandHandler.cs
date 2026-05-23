using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<User> userManager,
        IEmailSender emailSender,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Model.Email);

        if (user is null)
        {
            return Unit.Value;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        await _emailSender.SendPasswordResetAsync(
            user.Email!,
            user.UserName!,
            user.Id,
            token,
            user.PreferredLanguage,
            cancellationToken);

        _logger.LogInformation("Password reset requested for user {UserId}", user.Id);

        return Unit.Value;
    }
}
