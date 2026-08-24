using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.Features.Watchlist.Commands.RemoveWatchlistItem;
using MovieRecommendation.Application.Features.Watchlist.Commands.UpdateWatchlistStatus;
using MovieRecommendation.Application.Features.Watchlist.Commands.UpsertWatchlistItem;
using MovieRecommendation.Application.Features.Watchlist.Queries.GetMyWatchlist;
using MovieRecommendation.Application.Features.Watchlist.Queries.GetWatchlistItemStatus;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me/watchlist")]
public class WatchlistController : BaseController
{
    public WatchlistController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWatchlist([FromQuery] SearchWatchlistDto query, CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var page = await Mediator.Send(new GetMyWatchlistQuery(User.GetId(), query, lang), ct);
        return Ok(page);
    }

    [HttpGet("{movieId:guid}")]
    public async Task<IActionResult> GetStatus(Guid movieId, CancellationToken ct)
    {
        var status = await Mediator.Send(new GetWatchlistItemStatusQuery(User.GetId(), movieId), ct);
        return Ok(status);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(UpsertWatchlistDto request, CancellationToken ct)
    {
        await Mediator.Send(new UpsertWatchlistItemCommand(User.GetId(), request), ct);
        return NoContent();
    }

    [HttpPatch("{movieId:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid movieId, UpdateWatchlistStatusDto request, CancellationToken ct)
    {
        await Mediator.Send(new UpdateWatchlistStatusCommand(User.GetId(), movieId, request), ct);
        return NoContent();
    }

    [HttpDelete("{movieId:guid}")]
    public async Task<IActionResult> Remove(Guid movieId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveWatchlistItemCommand(User.GetId(), movieId), ct);
        return NoContent();
    }
}
