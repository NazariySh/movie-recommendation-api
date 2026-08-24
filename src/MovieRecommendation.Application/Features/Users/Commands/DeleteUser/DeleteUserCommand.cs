using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : ICommand;
