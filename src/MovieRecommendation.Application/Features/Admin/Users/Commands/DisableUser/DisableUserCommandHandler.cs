using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.DisableUser;

public class DisableUserCommandHandler : ICommandHandler<DisableUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<DisableUserCommandHandler> _logger;

    public DisableUserCommandHandler(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        ICacheService cache,
        ILogger<DisableUserCommandHandler> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(DisableUserCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorUserId == request.UserId)
        {
            throw new ForbiddenException("Admins cannot disable themselves.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User {request.UserId} not found.");

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to disable user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
        }

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(user.Id));

        _logger.LogInformation(
            "Actor {ActorUserId} disabled user {UserId} (reason: {Reason})",
            request.ActorUserId, user.Id, request.Reason ?? "n/a");

        return Unit.Value;
    }
}
