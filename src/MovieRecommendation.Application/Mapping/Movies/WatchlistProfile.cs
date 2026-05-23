using AutoMapper;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.DTOs.WatchHistory;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Mapping.Movies;

public class WatchlistProfile : Profile
{
    private const string FallbackLang = "en";

    public WatchlistProfile()
    {
        string lang = null!;

        CreateMap<WatchlistItem, WatchlistItemDto>()
            .ForMember(d => d.MovieKey, opt => opt.MapFrom(s => s.Movie.Key))
            .ForMember(d => d.PosterUrl, opt => opt.MapFrom(s => s.Movie.PosterUrl))
            .ForMember(d => d.MovieTitle, opt => opt.MapFrom(s =>
                s.Movie.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                ?? s.Movie.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                ?? s.Movie.OriginalTitle));

        CreateMap<WatchHistory, WatchHistoryDto>()
            .ForMember(d => d.MovieKey, opt => opt.MapFrom(s => s.Movie.Key))
            .ForMember(d => d.PosterUrl, opt => opt.MapFrom(s => s.Movie.PosterUrl))
            .ForMember(d => d.MovieTitle, opt => opt.MapFrom(s =>
                s.Movie.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                ?? s.Movie.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                ?? s.Movie.OriginalTitle));

        CreateMap<MovieRating, RatingDto>()
            .ForMember(d => d.UpdatedAt, opt => opt.MapFrom(s => s.UpdatedAt));

        CreateMap<MovieRating, UserRatingDto>()
            .ForMember(d => d.MovieKey, opt => opt.MapFrom(s => s.Movie.Key))
            .ForMember(d => d.PosterUrl, opt => opt.MapFrom(s => s.Movie.PosterUrl))
            .ForMember(d => d.MovieTitle, opt => opt.MapFrom(s =>
                s.Movie.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                ?? s.Movie.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                ?? s.Movie.OriginalTitle));
    }
}
