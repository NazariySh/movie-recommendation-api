using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Commands.UploadAvatar;

public record UploadAvatarCommand(
    Guid UserId,
    Stream Content,
    string ContentType,
    long Length,
    string FileName) : ICommand<UserProfileDto>;
