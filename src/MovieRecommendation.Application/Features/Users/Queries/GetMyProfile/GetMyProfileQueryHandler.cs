using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, UserProfileDto>
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;

    public GetMyProfileQueryHandler(UserManager<User> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new NotFoundException($"User with id {request.UserId} not found");

        var roles = await _userManager.GetRolesAsync(user);
        var dto = _mapper.Map<UserProfileDto>(user);
        dto.Roles = roles.ToList();

        return dto;
    }
}
