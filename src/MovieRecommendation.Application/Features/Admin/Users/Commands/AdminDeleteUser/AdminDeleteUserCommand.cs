using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.AdminDeleteUser;

public record AdminDeleteUserCommand(Guid ActorUserId, Guid UserId) : ICommand;
