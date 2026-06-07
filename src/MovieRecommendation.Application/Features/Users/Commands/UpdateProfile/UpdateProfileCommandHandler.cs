using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UpdateProfileCommandHandler(UserManager<User> userManager, IUserRepository userRepository, IMapper mapper)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user is null)
        {
            throw new NotFoundException($"User with id {request.UserId} not found");
        }

        var requestedUsername = request.Model.Username.Trim();

        if (!string.Equals(user.UserName, requestedUsername, StringComparison.OrdinalIgnoreCase))
        {
            if (!await _userRepository.IsUsernameUniqueAsync(requestedUsername, cancellationToken))
            {
                throw new AlreadyExistsException($"Username '{requestedUsername}' is already taken.");
            }

            var setUsername = await _userManager.SetUserNameAsync(user, requestedUsername);
            setUsername.EnsureSucceeded("Failed to update username");
        }

        user.Bio = string.IsNullOrWhiteSpace(request.Model.Bio) ? null : request.Model.Bio.Trim();
        user.PreferredLanguage = request.Model.PreferredLanguage;
        user.UpdatedAt = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(user);
        update.EnsureSucceeded("Failed to update profile");

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserProfileDto>(user);
        dto.Roles = roles.ToList();

        return dto;
    }
}
