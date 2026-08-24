using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Auth.Commands.Register;

public record RegisterCommand(RegisterRequestDto Model, RoleType RoleType = RoleType.User) : ICommand;
