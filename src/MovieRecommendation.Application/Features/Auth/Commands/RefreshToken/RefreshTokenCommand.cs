using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;

namespace MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand : ICommand<LoginResponseDto>;
