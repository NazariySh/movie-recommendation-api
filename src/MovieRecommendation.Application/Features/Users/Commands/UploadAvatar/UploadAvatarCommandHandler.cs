using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Commands.UploadAvatar;

public class UploadAvatarCommandHandler : ICommandHandler<UploadAvatarCommand, UserProfileDto>
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    private readonly UserManager<User> _userManager;
    private readonly IBlobStorageService _blobStorage;
    private readonly IMapper _mapper;

    public UploadAvatarCommandHandler(UserManager<User> userManager, IBlobStorageService blobStorage, IMapper mapper)
    {
        _userManager = userManager;
        _blobStorage = blobStorage;
        _mapper = mapper;
    }

    public async Task<UserProfileDto> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        if (!_blobStorage.IsConfigured)
        {
            throw new DomainException(
                System.Net.HttpStatusCode.ServiceUnavailable,
                "Avatar uploads are unavailable: blob storage is not configured.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User with id {request.UserId} not found");

        if (!AllowedContentTypes.TryGetValue(request.ContentType, out var extension))
        {
            throw new DomainException(
                System.Net.HttpStatusCode.UnsupportedMediaType,
                $"Unsupported content type '{request.ContentType}'. Allowed: jpeg, png, webp.");
        }

        var path = $"avatars/{user.Id}.{extension}";
        var url = await _blobStorage.UploadAsync(path, request.Content, request.ContentType, cancellationToken);

        user.AvatarUrl = url;
        user.UpdatedAt = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(user);
        update.EnsureSucceeded("Failed to persist avatar URL");

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserProfileDto>(user);
        dto.Roles = roles.ToList();

        return dto;
    }
}
