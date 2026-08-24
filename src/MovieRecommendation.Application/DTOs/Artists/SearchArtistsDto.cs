namespace MovieRecommendation.Application.DTOs.Artists;

public class SearchArtistsDto : PaginationQuery
{
    public string? Search { get; set; }

    public string? Role { get; set; }

    public string? SortBy { get; set; }
}
