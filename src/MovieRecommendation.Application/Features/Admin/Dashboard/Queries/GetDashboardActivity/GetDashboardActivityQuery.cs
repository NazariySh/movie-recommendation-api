using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Admin;

namespace MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardActivity;

public record GetDashboardActivityQuery(int Days) : IQuery<DashboardActivityDto>;
