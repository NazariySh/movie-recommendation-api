using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, UpdateProfileDto Model) : ICommand<UserProfileDto>;
