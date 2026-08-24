using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Search;

public class MovieSuggestionDto
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public TitleType Type { get; set; }

    public string Title { get; set; } = null!;

    public int? ReleaseYear { get; set; }

    public string? PosterUrl { get; set; }
}
