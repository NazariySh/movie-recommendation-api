using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Interfaces;

public class ExternalMovieResult
{
    public string ImdbId { get; set; } = null!;

    public int? TmdbId { get; set; }

    public TitleType Type { get; set; } = TitleType.Movie;

    public string Title { get; set; } = null!;

    public string OriginalTitle { get; set; } = null!;

    public string OriginalLang { get; set; } = "en";

    public string? Overview { get; set; }

    public string? Tagline { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? Runtime { get; set; }

    public int? SeasonsCount { get; set; }

    public int? EpisodesCount { get; set; }

    public bool? IsOngoing { get; set; }

    public MovieStatus Status { get; set; } = MovieStatus.Unknown;

    public string? PosterUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public string? TrailerYoutubeId { get; set; }

    public IReadOnlyList<string> Genres { get; set; } = [];

    public IReadOnlyList<ExternalMovieCast> Cast { get; set; } = [];

    public IReadOnlyList<ExternalMovieSeason> Seasons { get; set; } = [];
}

public class ExternalMovieSeason
{
    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public string? Overview { get; set; }

    public string? PosterUrl { get; set; }

    public int EpisodeCount { get; set; }

    public DateTime? AirDate { get; set; }

    public decimal? VoteAverage { get; set; }
}
