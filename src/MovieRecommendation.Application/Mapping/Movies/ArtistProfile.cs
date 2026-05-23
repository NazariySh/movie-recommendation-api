using AutoMapper;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Mapping.Movies;

public class ArtistProfile : Profile
{
    public ArtistProfile()
    {
        CreateMap<Person, ArtistDto>()
            .ForMember(d => d.Roles, opt => opt.Ignore())
            .ForMember(d => d.MovieCount, opt => opt.Ignore());

        CreateMap<Person, ArtistDetailDto>()
            .ForMember(d => d.Roles, opt => opt.Ignore())
            .ForMember(d => d.Filmography, opt => opt.Ignore());

        CreateMap<Person, Person>();
    }
}
