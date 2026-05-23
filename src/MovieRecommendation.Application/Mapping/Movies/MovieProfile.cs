using AutoMapper;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Mapping.Movies;

public class MovieProfile : Profile
{
    private const string FallbackLang = LanguageCodes.Default;

    public MovieProfile()
    {
        string lang = null!;
        Guid currentUserId = default;

        CreateMap<CreateMovieDto, Movie>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.MovieGenres, opt => opt.Ignore())
            .ForMember(d => d.Translations, opt => opt.Ignore())
            .ForMember(d => d.MovieCasts, opt => opt.Ignore())
            .ForMember(d => d.Ratings, opt => opt.Ignore())
            .ForMember(d => d.Reviews, opt => opt.Ignore())
            .ForMember(d => d.Keywords, opt => opt.Ignore())
            .ForMember(d => d.Embedding, opt => opt.Ignore())
            .ForMember(d => d.AverageRating, opt => opt.Ignore())
            .ForMember(d => d.RatingsCount, opt => opt.Ignore())
            .ForMember(d => d.Seasons, opt => opt.Ignore())
            .ForMember(d => d.WatchHistory, opt => opt.Ignore());

        CreateMap<UpdateMovieDto, Movie>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.MovieGenres, opt => opt.Ignore())
            .ForMember(d => d.Translations, opt => opt.Ignore())
            .ForMember(d => d.MovieCasts, opt => opt.Ignore())
            .ForMember(d => d.Ratings, opt => opt.Ignore())
            .ForMember(d => d.Reviews, opt => opt.Ignore())
            .ForMember(d => d.Keywords, opt => opt.Ignore())
            .ForMember(d => d.Embedding, opt => opt.Ignore())
            .ForMember(d => d.AverageRating, opt => opt.Ignore())
            .ForMember(d => d.RatingsCount, opt => opt.Ignore())
            .ForMember(d => d.Seasons, opt => opt.Ignore())
            .ForMember(d => d.WatchHistory, opt => opt.Ignore());


        CreateMap<MovieCast, MovieCastDto>()
            .ForMember(d => d.PersonName, opt => opt.MapFrom(s => s.Person.Name))
            .ForMember(d => d.PersonId, opt => opt.MapFrom(s => s.PersonId))
            .ForMember(d => d.PhotoUrl, opt => opt.MapFrom(s => s.Person.PhotoUrl));

        CreateMap<Season, SeasonDto>()
            .ForMember(d => d.MyRating, opt => opt.MapFrom(s =>
                currentUserId == Guid.Empty
                    ? (decimal?)null
                    : s.Ratings.Where(r => r.UserId == currentUserId).Select(r => (decimal?)r.Score).FirstOrDefault()));

        CreateMap<SeasonRating, SeasonRatingDto>();

        CreateMap<Movie, MovieDto>()
            .ForMember(d => d.Title, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                ?? s.OriginalTitle))
            .ForMember(d => d.Overview, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Overview).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Overview).FirstOrDefault()))
            .ForMember(d => d.Rating, opt => opt.MapFrom(s =>
                s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0m))
            .ForMember(d => d.SeasonsCount, opt => opt.MapFrom(s => (int?)s.Seasons.Count))
            .ForMember(d => d.EpisodesCount, opt => opt.Ignore())
            .ForMember(d => d.RecommendationReason, opt => opt.Ignore())
            .ForMember(d => d.Genres, opt => opt.MapFrom(s =>
                s.MovieGenres.Select(mg =>
                    mg.Genre.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Slug)));

        CreateMap<Movie, MovieListItemDto>()
            .ForMember(d => d.Title, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                ?? s.OriginalTitle))
            .ForMember(d => d.Overview, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Overview).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Overview).FirstOrDefault()))
            .ForMember(d => d.AverageRating, opt => opt.MapFrom(s =>
                s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0m))
            .ForMember(d => d.RatingsCount, opt => opt.MapFrom(s => s.Ratings.Count()))
            .ForMember(d => d.ReleaseYear, opt => opt.MapFrom(s =>
                s.ReleaseDate.HasValue ? (int?)s.ReleaseDate.Value.Year : null))
            .ForMember(d => d.Genres, opt => opt.MapFrom(s =>
                s.MovieGenres.Select(mg =>
                    mg.Genre.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Slug)));

        CreateMap<Movie, MovieDetailDto>()
            .ForMember(d => d.Title, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Title).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Title).FirstOrDefault()
                ?? s.OriginalTitle))
            .ForMember(d => d.Overview, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Overview).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Overview).FirstOrDefault()))
            .ForMember(d => d.Tagline, opt => opt.MapFrom(s =>
                s.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Tagline).FirstOrDefault()
                ?? s.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Tagline).FirstOrDefault()))
            .ForMember(d => d.VoteAverage, opt => opt.MapFrom(s =>
                s.Ratings.Any() ? s.Ratings.Average(r => r.Score) : 0m))
            .ForMember(d => d.VoteCount, opt => opt.MapFrom(s => s.Ratings.Count()))
            .ForMember(d => d.Genres, opt => opt.MapFrom(s =>
                s.MovieGenres.Select(mg =>
                    mg.Genre.Translations.Where(t => t.LanguageCode == lang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Translations.Where(t => t.LanguageCode == FallbackLang).Select(t => t.Name).FirstOrDefault()
                    ?? mg.Genre.Slug)))
            .ForMember(d => d.Keywords, opt => opt.MapFrom(s => s.Keywords.Select(k => k.Name)))
            .ForMember(d => d.Seasons, opt => opt.MapFrom(s => s.Seasons.OrderBy(season => season.SeasonNumber)))
            .ForMember(d => d.Casts, opt => opt.MapFrom(s => s.MovieCasts));
    }
}
