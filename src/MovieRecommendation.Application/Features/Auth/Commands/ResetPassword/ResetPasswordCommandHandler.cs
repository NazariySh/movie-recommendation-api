using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Model.UserId.ToString());
        if (user is null)
        {
            _logger.LogWarning("Password reset attempted for missing user {UserId}", request.Model.UserId);
            throw new UnauthorizedAccessException("Invalid reset request.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Model.Token, request.Model.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Password reset failed for {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new UnauthorizedAccessException("Invalid reset request.");
        }

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        _logger.LogInformation("Password reset for user {UserId}", user.Id);

        return Unit.Value;
    }
}
