using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        UserManager<User> userManager,
        IEmailSender emailSender,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Unit> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Model.UserId.ToString());
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return Unit.Value;
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Model.Token);
        result.EnsureSucceeded("Email verification failed.");

        await _emailSender.SendWelcomeAsync(user.Email!, user.UserName!, user.PreferredLanguage, cancellationToken);

        _logger.LogInformation("Email verified for user {UserId}", user.Id);

        return Unit.Value;
    }
}
