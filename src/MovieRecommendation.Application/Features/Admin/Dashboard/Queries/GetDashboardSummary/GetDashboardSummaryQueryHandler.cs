using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IAdminDashboardRepository _repository;
    private readonly ICacheService _cache;

    public GetDashboardSummaryQueryHandler(IAdminDashboardRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        return await _cache.GetOrSetAsync(
            AdminCacheKeys.DashboardSummary,
            ct => _repository.GetSummaryAsync(ct),
            DashboardWindows.SummaryCacheTtl,
            cancellationToken);
    }
}
