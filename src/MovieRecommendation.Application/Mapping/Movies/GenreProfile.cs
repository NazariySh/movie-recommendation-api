using AutoMapper;
using MovieRecommendation.Application.DTOs.Genres;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Mapping.Movies;

public class GenreProfile : Profile
{
    private const string FallbackLang = LanguageCodes.Default;

    public GenreProfile()
    {
        string lang = null!;

        CreateMap<Genre, GenreDto>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Name).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Name).FirstOrDefault()
                ?? s.Slug));

        CreateMap<Genre, Genre>()
            .ForMember(d => d.Translations, opt => opt.Ignore());
    }
}
