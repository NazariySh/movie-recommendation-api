using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Commands.CreateArtist;
using MovieRecommendation.Application.Features.Artists.Commands.DeleteArtist;
using MovieRecommendation.Application.Features.Artists.Commands.UpdateArtist;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/artists")]
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.Moderator}")]
public class AdminArtistsController : BaseController
{
    public AdminArtistsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateArtistDto request, CancellationToken ct)
    {
        var id = await Mediator.Send(new CreateArtistCommand(request), ct);
        return CreatedAtAction(
            actionName: nameof(ArtistsController.GetArtistById),
            controllerName: "Artists",
            routeValues: new { id },
            value: new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateArtistDto request, CancellationToken ct)
    {
        await Mediator.Send(new UpdateArtistCommand(id, request), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteArtistCommand(id), ct);
        return NoContent();
    }
}
