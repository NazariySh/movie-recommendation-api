using MediatR;
using Microsoft.AspNetCore.Identity;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICacheService _cache;

    public DeleteUserCommandHandler(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        ICacheService cache)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            throw new NotFoundException($"User with id {request.UserId} not found");
        }

        if (user.IsDeleted)
        {
            return Unit.Value;
        }

        UserAnonymizer.Anonymize(user);

        var result = await _userManager.UpdateAsync(user);
        result.EnsureSucceeded("Failed to delete user");

        await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);

        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(user.Id));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(user.Id));

        return Unit.Value;
    }
}
