using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.DisableUser;

public record DisableUserCommand(Guid UserId, string? Reason) : ICommand;
