using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;
using MovieRecommendation.Application.Features.Movies.Commands.DeleteMovie;
using MovieRecommendation.Application.Features.Movies.Commands.RegenerateEmbedding;
using MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;
using MovieRecommendation.Application.Features.Movies.Queries.GetAdminMovieById;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/movies")]
[Authorize(Roles = $"{RoleTypes.Admin},{RoleTypes.Moderator}")]
public class AdminMoviesController : BaseController
{
    private const long MaxUploadBytes = 16 * 1024 * 1024;

    public AdminMoviesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await Mediator.Send(new GetAdminMovieByIdQuery(id), ct);
        return Ok(dto);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Create(
        [FromForm] CreateMovieDto request,
        IFormFile? poster,
        IFormFile? backdrop,
        CancellationToken ct)
    {
        await using var posterStream = OpenUpload(poster);
        await using var backdropStream = OpenUpload(backdrop);

        var id = await Mediator.Send(
            new CreateMovieCommand(request, AsImageUpload(poster, posterStream), AsImageUpload(backdrop, backdropStream)),
            ct);

        return CreatedAtAction(
            actionName: nameof(MoviesController.GetMovieById),
            controllerName: "Movies",
            routeValues: new { id },
            value: new { id });
    }

    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromForm] UpdateMovieDto request,
        IFormFile? poster,
        IFormFile? backdrop,
        CancellationToken ct)
    {
        await using var posterStream = OpenUpload(poster);
        await using var backdropStream = OpenUpload(backdrop);

        await Mediator.Send(
            new UpdateMovieCommand(id, request, AsImageUpload(poster, posterStream), AsImageUpload(backdrop, backdropStream)),
            ct);

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
