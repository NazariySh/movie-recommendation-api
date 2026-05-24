using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.UpdateUserRoles;

public record UpdateUserRolesCommand(Guid UserId, IReadOnlyList<string> Roles) : ICommand;
