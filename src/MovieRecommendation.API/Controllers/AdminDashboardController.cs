using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardActivity;
using MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = RoleTypes.Admin)]
public class AdminDashboardController : BaseController
{
    public AdminDashboardController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var summary = await Mediator.Send(new GetDashboardSummaryQuery(), ct);
        return Ok(summary);
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var activity = await Mediator.Send(new GetDashboardActivityQuery(days), ct);
        return Ok(activity);
    }
}
