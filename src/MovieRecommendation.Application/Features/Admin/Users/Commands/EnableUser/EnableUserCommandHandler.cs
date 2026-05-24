using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.EnableUser;

public class EnableUserCommandHandler : ICommandHandler<EnableUserCommand>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<EnableUserCommandHandler> _logger;

    public EnableUserCommandHandler(
        UserManager<User> userManager,
        IUserRepository userRepository,
        ILogger<EnableUserCommandHandler> logger)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(EnableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetTrackedIncludingDeletedAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {request.UserId} not found.");

        user.IsDeleted = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.SetLockoutEndDateAsync(user, null);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to enable user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _logger.LogInformation("Enabled user {UserId}", user.Id);

        return Unit.Value;
    }
}
