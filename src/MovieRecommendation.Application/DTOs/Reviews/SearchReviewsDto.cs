namespace MovieRecommendation.Application.DTOs.Reviews;

public class SearchReviewsDto : PaginationQuery
{
    public string? Sort { get; set; }
}
