using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetModelStatus;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/recommendations")]
[Authorize(Roles = $"{RoleTypes.Admin}")]
public class AdminRecommendationsController : BaseController
{
    public AdminRecommendationsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("model-status")]
    public async Task<IActionResult> GetModelStatus(CancellationToken ct)
    {
        var status = await Mediator.Send(new GetModelStatusQuery(), ct);
        return Ok(status);
    }

    [HttpPost("retrain")]
    public async Task<IActionResult> TriggerRetrain([FromQuery] bool wait = false, CancellationToken ct = default)
    {
        var result = await Mediator.Send(new TriggerRetrainCommand(wait), ct);
        return Accepted(result);
    }
}
