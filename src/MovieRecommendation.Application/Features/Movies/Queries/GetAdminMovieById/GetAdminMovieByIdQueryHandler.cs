using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetAdminMovieById;

public class GetAdminMovieByIdQueryHandler : IQueryHandler<GetAdminMovieByIdQuery, AdminMovieDetailDto>
{
    private readonly IMovieRepository _movieRepository;

    public GetAdminMovieByIdQueryHandler(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<AdminMovieDetailDto> Handle(GetAdminMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetForAdminEditAsync(request.Id, cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with ID {request.Id} not found");
        }

        return new AdminMovieDetailDto
        {
            Id = movie.Id,
            Key = movie.Key,
            Type = movie.Type,
            OriginalTitle = movie.OriginalTitle,
            OriginalLang = movie.OriginalLang,
            ImdbId = movie.ImdbId,
            TmdbId = movie.TmdbId,
            PosterUrl = movie.PosterUrl,
            BackdropUrl = movie.BackdropUrl,
            TrailerYoutubeId = movie.TrailerYoutubeId,
            ReleaseDate = movie.ReleaseDate,
            Status = movie.Status,
            Runtime = movie.Runtime,
            SeasonsCount = movie.Seasons?.Count,
            IsOngoing = movie.IsOngoing,
            GenreIds = movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
            Translations = movie.Translations
                .Select(t => new MovieTranslationDto
                {
                    LanguageCode = t.LanguageCode,
                    Title = t.Title,
                    Overview = t.Overview,
                    Tagline = t.Tagline,
                })
                .ToList(),
        };
    }
}
