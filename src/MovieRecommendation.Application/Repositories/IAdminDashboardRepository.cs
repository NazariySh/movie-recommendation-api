using MovieRecommendation.Application.DTOs.Admin;

namespace MovieRecommendation.Application.Repositories;

public interface IAdminDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);

    Task<DashboardActivityDto> GetActivityAsync(int days, CancellationToken ct = default);
}
