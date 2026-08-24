namespace MovieRecommendation.Application.DTOs.Admin;

public class SearchAdminUsersDto : PaginationQuery
{
    public string? Search { get; set; }

    public string? Role { get; set; }

    public bool? IsLocked { get; set; }

    public bool? IsActive { get; set; }

    public string? Sort { get; set; }

    public string? Order { get; set; }
}
