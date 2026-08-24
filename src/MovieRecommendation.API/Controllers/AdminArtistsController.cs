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
    private const long MaxUploadBytes = 8 * 1024 * 1024;

    public AdminArtistsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create([FromForm] CreateArtistDto request, IFormFile? photo, CancellationToken ct)
    {
        await using var photoStream = OpenUpload(photo);

        var id = await Mediator.Send(new CreateArtistCommand(request, AsImageUpload(photo, photoStream)), ct);

        return CreatedAtAction(
            actionName: nameof(ArtistsController.GetArtistById),
            controllerName: "Artists",
            routeValues: new { id },
            value: new { id });
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateArtistDto request, IFormFile? photo, CancellationToken ct)
    {
        await using var photoStream = OpenUpload(photo);

        await Mediator.Send(new UpdateArtistCommand(id, request, AsImageUpload(photo, photoStream)), ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteArtistCommand(id), ct);
        return NoContent();
    }
}
