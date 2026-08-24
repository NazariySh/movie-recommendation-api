using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : ICommand;
