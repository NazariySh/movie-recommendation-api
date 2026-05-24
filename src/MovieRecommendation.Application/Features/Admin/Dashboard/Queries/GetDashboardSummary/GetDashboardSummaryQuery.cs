using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Admin;

namespace MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;
