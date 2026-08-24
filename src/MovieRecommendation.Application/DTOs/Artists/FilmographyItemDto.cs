using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Artists;

public class FilmographyItemDto
{
    public Guid MovieId { get; set; }

    public string MovieKey { get; set; } = null!;

    public string MovieTitle { get; set; } = null!;

    public TitleType MovieType { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public string? PosterUrl { get; set; }

    public string Role { get; set; } = null!;

    public string? Character { get; set; }

    public int? CastOrder { get; set; }

    public decimal Rating { get; set; }

    public IReadOnlyList<string> Genres { get; set; } = [];
}
