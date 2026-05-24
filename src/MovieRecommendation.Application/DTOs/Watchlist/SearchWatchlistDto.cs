using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.DTOs.Watchlist;

public class SearchWatchlistDto : PaginationQuery
{
    public WatchlistStatus? Status { get; set; }
}
