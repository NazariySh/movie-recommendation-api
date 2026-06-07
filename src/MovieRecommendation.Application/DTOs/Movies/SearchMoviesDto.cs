using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Movies;

public class SearchMoviesDto : PaginationQuery
{
    public string? Search { get; set; }

    public bool Semantic { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; }

    public TitleType? Type { get; set; }

    public List<int>? Genres { get; set; }

    public List<string>? GenreSlugs { get; set; }

    public List<int>? ReleaseYears { get; set; }

    public List<int>? Countries { get; set; }

    public int? YearFrom { get; set; }

    public int? YearTo { get; set; }

    public decimal? MinRating { get; set; }

    public decimal? MaxRating { get; set; }

    public int? MinRatingsCount { get; set; }

    public int? RuntimeMin { get; set; }

    public int? RuntimeMax { get; set; }

    public string? Language { get; set; }
}
