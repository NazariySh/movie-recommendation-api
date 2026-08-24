using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;

namespace MovieRecommendation.Application.Features.Auth.Commands.GoogleAuth;

public record GoogleAuthCommand(GoogleAuthRequestDto Model) : ICommand<LoginResponseDto>;
