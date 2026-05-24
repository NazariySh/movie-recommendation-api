using MovieRecommendation.Application.Abstractions.Messaging;

namespace MovieRecommendation.Application.Features.Admin.Users.Commands.ForceResetPassword;

public record ForceResetPasswordCommand(Guid UserId) : ICommand;
