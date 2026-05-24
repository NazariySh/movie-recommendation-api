using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.WatchHistory;
using MovieRecommendation.Application.Features.WatchHistory.Commands.AddWatchHistory;
using MovieRecommendation.Application.Features.WatchHistory.Queries.GetMyWatchHistory;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me/watched")]
public class WatchHistoryController : BaseController
{
    public WatchHistoryController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWatchHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var page = await Mediator.Send(new GetMyWatchHistoryQuery(User.GetId(), pageNumber, pageSize, lang), ct);
        return Ok(page);
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateWatchHistoryDto request, CancellationToken ct)
    {
        await Mediator.Send(new AddWatchHistoryCommand(User.GetId(), request), ct);
        return NoContent();
    }
}
