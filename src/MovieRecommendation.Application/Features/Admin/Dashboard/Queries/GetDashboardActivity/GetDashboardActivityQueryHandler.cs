using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardActivity;

public class GetDashboardActivityQueryHandler : IQueryHandler<GetDashboardActivityQuery, DashboardActivityDto>
{
    private readonly IAdminDashboardRepository _repository;
    private readonly ICacheService _cache;

    public GetDashboardActivityQueryHandler(IAdminDashboardRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public Task<DashboardActivityDto> Handle(GetDashboardActivityQuery request, CancellationToken cancellationToken)
    {
        var days = request.Days <= 0
            ? DashboardWindows.ActivityDaysDefault
            : Math.Min(request.Days, DashboardWindows.ActivityDaysMax);

        return _cache.GetOrSetAsync(
            AdminCacheKeys.DashboardActivity(days),
            ct => _repository.GetActivityAsync(days, ct),
            DashboardWindows.ActivityCacheTtl,
            cancellationToken);
    }
}
