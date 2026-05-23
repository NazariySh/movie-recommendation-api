using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Queries.GetAllArtists;
using MovieRecommendation.Application.Features.Artists.Queries.GetArtistById;
using MovieRecommendation.Application.Features.Artists.Queries.GetArtistFilmography;
using MovieRecommendation.Application.Features.Artists.Queries.GetPopularArtists;

namespace MovieRecommendation.API.Controllers;

public class ArtistsController : BaseController
{
    public ArtistsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllArtists([FromQuery] SearchArtistsDto searchDto, CancellationToken ct)
    {
        var artists = await Mediator.Send(new GetAllArtistsQuery(searchDto), ct);
        return Ok(artists);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int count = 12, CancellationToken ct = default)
    {
        var artists = await Mediator.Send(new GetPopularArtistsQuery(count), ct);
        return Ok(artists);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetArtistById(Guid id, CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var artist = await Mediator.Send(new GetArtistByIdQuery(id, lang), ct);
        return Ok(artist);
    }

    [HttpGet("{id:guid}/filmography")]
    public async Task<IActionResult> GetFilmography(Guid id, [FromQuery] string? role, CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var items = await Mediator.Send(new GetArtistFilmographyQuery(id, role, lang), ct);
        return Ok(items);
    }
}
