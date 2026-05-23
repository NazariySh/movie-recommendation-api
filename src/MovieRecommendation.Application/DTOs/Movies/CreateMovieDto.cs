using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Movies;

public class CreateMovieDto
{
    public string Key { get; set; } = null!;

    public TitleType Type { get; set; } = TitleType.Movie;

    public string OriginalTitle { get; set; } = null!;

    public string OriginalLang { get; set; } = "en";

    public string? ImdbId { get; set; }

    public int? TmdbId { get; set; }

    public string? PosterUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public string? TrailerYoutubeId { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public MovieStatus Status { get; set; } = MovieStatus.Released;

    public int? Runtime { get; set; }

    public int? SeasonsCount { get; set; }

    public int? EpisodesCount { get; set; }

    public bool? IsOngoing { get; set; }

    public List<int> GenreIds { get; set; } = [];

    public List<MovieTranslationDto> Translations { get; set; } = [];
}
