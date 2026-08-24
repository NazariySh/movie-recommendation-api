using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;

namespace MovieRecommendation.Application.Features.Auth.Commands.ResendVerification;

public record ResendVerificationCommand(ResendVerificationRequestDto Model) : ICommand;
