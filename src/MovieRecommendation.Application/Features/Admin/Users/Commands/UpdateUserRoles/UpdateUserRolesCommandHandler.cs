using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.UpdateUserRoles;

public class UpdateUserRolesCommandHandler : ICommandHandler<UpdateUserRolesCommand>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(RoleType.Admin),
        nameof(RoleType.Moderator),
        nameof(RoleType.User),
    };

    private readonly UserManager<User> _userManager;
    private readonly ILogger<UpdateUserRolesCommandHandler> _logger;

    public UpdateUserRolesCommandHandler(
        UserManager<User> userManager,
        ILogger<UpdateUserRolesCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User {request.UserId} not found.");

        var requested = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invalid = requested.Where(r => !AllowedRoles.Contains(r)).ToList();
        if (invalid.Count > 0)
        {
            throw new ForbiddenException($"Unknown role(s): {string.Join(", ", invalid)}.");
        }

        var current = await _userManager.GetRolesAsync(user);
        var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();
        var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to remove roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
            }
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to add roles: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
            }
        }

        _logger.LogInformation(
            "Updated roles for user {UserId}: -[{Removed}] +[{Added}]",
            user.Id, string.Join(",", toRemove), string.Join(",", toAdd));

        return Unit.Value;
    }
}
