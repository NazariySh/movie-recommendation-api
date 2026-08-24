using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;

namespace MovieRecommendation.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginRequestDto Model) : ICommand<LoginResponseDto>;
