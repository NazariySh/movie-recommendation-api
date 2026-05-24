using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;
using MovieRecommendation.Application.Features.Movies.Commands.DeleteMovie;
using MovieRecommendation.Application.Features.Movies.Commands.RegenerateEmbedding;
using MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/movies")]
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.Moderator}")]
public class AdminMoviesController : BaseController
{
    public AdminMoviesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMovieDto request, CancellationToken ct)
    {
        var id = await Mediator.Send(new CreateMovieCommand(request), ct);
        return CreatedAtAction(
            actionName: nameof(MoviesController.GetMovieById),
            controllerName: "Movies",
            routeValues: new { id },
            value: new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMovieDto request, CancellationToken ct)
    {
        await Mediator.Send(new UpdateMovieCommand(id, request), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteMovieCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/regenerate-embedding")]
    public async Task<IActionResult> RegenerateEmbedding(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new RegenerateEmbeddingCommand(id), ct);
        return NoContent();
    }
}
