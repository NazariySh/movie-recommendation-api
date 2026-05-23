using AutoMapper;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Mapping.Users;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, AuthUserDto>()
            .ForMember(d => d.Username, opt => opt.MapFrom(s => s.UserName))
            .ForMember(d => d.Roles, opt => opt.Ignore());

        CreateMap<User, UserProfileDto>()
            .ForMember(d => d.Username, opt => opt.MapFrom(s => s.UserName ?? string.Empty))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email ?? string.Empty))
            .ForMember(d => d.Roles, opt => opt.Ignore());
    }
}
