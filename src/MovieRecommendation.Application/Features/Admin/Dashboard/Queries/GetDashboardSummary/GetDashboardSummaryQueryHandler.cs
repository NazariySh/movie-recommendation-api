using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    private readonly IAdminDashboardRepository _repository;
    private readonly ICacheService _cache;

    public GetDashboardSummaryQueryHandler(IAdminDashboardRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        return _cache.GetOrSetAsync(
            AdminCacheKeys.DashboardSummary,
            ct => _repository.GetSummaryAsync(ct),
            CacheTtl,
            cancellationToken);
    }
}
