using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Movies;

public class MovieListItemDto
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public TitleType Type { get; set; }

    public string Title { get; set; } = null!;

    public string OriginalTitle { get; set; } = null!;

    public string? Overview { get; set; }

    public string? PosterUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public int? ReleaseYear { get; set; }

    public int? Runtime { get; set; }

    public string OriginalLang { get; set; } = null!;

    public decimal AverageRating { get; set; }

    public int RatingsCount { get; set; }

    public IReadOnlyList<string> Genres { get; set; } = [];
}
