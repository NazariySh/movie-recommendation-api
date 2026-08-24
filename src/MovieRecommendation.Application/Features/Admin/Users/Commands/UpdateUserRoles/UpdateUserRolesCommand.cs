using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.UpdateUserRoles;

public record UpdateUserRolesCommand(Guid ActorUserId, Guid UserId, IReadOnlyList<string> Roles) : ICommand;
