using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;

namespace MovieRecommendation.Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(VerifyEmailRequestDto Model) : ICommand;
