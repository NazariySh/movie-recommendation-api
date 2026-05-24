using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Movies;

public class MovieDto
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public TitleType Type { get; set; }

    public string Title { get; set; } = null!;

    public string OriginalTitle { get; set; } = null!;

    public string? Overview { get; set; }

    public MovieStatus Status { get; set; }

    public string? PosterUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public decimal Rating { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? Runtime { get; set; }

    public int? SeasonsCount { get; set; }

    public int? EpisodesCount { get; set; }

    public bool? IsOngoing { get; set; }

    public IReadOnlyList<string> Genres { get; set; } = [];

    public RecommendationReason? RecommendationReason { get; set; }
}
