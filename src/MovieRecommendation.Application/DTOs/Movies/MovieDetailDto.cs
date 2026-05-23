using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Movies;

public class MovieDetailDto
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public TitleType Type { get; set; }

    public string Title { get; set; } = null!;

    public string OriginalTitle { get; set; } = null!;

    public string? Tagline { get; set; }

    public string? Overview { get; set; }

    public string? PosterUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public string? TrailerYoutubeId { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? Runtime { get; set; }

    public bool? IsOngoing { get; set; }

    public string OriginalLang { get; set; } = null!;

    public MovieStatus Status { get; set; }

    public decimal VoteAverage { get; set; }

    public int VoteCount { get; set; }

    public string? ImdbId { get; set; }

    public int? TmdbId { get; set; }

    public IReadOnlyList<string> Genres { get; set; } = [];

    public IReadOnlyList<string> Keywords { get; set; } = [];

    public IReadOnlyList<MovieCastDto> Casts { get; set; } = [];

    public IReadOnlyList<SeasonDto> Seasons { get; set; } = [];
}
