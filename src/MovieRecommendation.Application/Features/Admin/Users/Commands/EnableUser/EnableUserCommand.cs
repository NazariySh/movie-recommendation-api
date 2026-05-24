using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.EnableUser;

public record EnableUserCommand(Guid UserId) : ICommand;
