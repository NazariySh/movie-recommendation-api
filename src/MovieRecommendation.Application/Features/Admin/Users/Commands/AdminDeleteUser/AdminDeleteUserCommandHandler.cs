using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.Features.Users;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.AdminDeleteUser;

public class AdminDeleteUserCommandHandler : ICommandHandler<AdminDeleteUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<AdminDeleteUserCommandHandler> _logger;

    public AdminDeleteUserCommandHandler(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        ICacheService cache,
        ILogger<AdminDeleteUserCommandHandler> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(AdminDeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (request.ActorUserId == request.UserId)
        {
            throw new ForbiddenException("Admins cannot delete themselves. Use the profile delete endpoint instead.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User {request.UserId} not found.");

        if (user.IsDeleted)
        {
            return Unit.Value;
        }

        UserAnonymizer.Anonymize(user);

        var result = await _userManager.UpdateAsync(user);
        result.EnsureSucceeded("Failed to anonymize user");

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(user.Id));

        _logger.LogInformation("Actor {ActorUserId} anonymized user {UserId}", request.ActorUserId, user.Id);

        return Unit.Value;
    }
}
