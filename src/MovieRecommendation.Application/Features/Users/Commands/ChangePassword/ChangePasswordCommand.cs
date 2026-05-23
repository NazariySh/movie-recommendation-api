using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Commands.ChangePassword;

public record ChangePasswordCommand(Guid UserId, ChangePasswordRequestDto Model) : ICommand;
