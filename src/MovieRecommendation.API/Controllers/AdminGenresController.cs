using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.DTOs.Genres;
using MovieRecommendation.Application.Features.Genres.Commands.CreateGenre;
using MovieRecommendation.Application.Features.Genres.Commands.DeleteGenre;
using MovieRecommendation.Application.Features.Genres.Commands.UpdateGenre;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/genres")]
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.Moderator}")]
public class AdminGenresController : BaseController
{
    public AdminGenresController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGenreDto request, CancellationToken ct)
    {
        var id = await Mediator.Send(new CreateGenreCommand(request), ct);
        return Ok(new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateGenreDto request, CancellationToken ct)
    {
        await Mediator.Send(new UpdateGenreCommand(id, request), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteGenreCommand(id), ct);
        return NoContent();
    }
}
