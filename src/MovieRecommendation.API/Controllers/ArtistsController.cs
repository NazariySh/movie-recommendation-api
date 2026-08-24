using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Artists;
using MovieRecommendation.Application.Features.Artists.Queries.GetAllArtists;
using MovieRecommendation.Application.Features.Artists.Queries.GetArtistById;

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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetArtistById(Guid id, CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var artist = await Mediator.Send(new GetArtistByIdQuery(id, lang), ct);
        return Ok(artist);
    }
}
