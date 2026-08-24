using MediatR;
using Microsoft.AspNetCore.Identity;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ChangePasswordCommandHandler(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            throw new NotFoundException($"User with id {request.UserId} not found");
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            request.Model.CurrentPassword,
            request.Model.NewPassword);

        result.EnsureSucceeded("Failed to change password");

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        return Unit.Value;
    }
}
