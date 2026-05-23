using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.Features.Genres.Queries.GetAllGenres;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/genres")]
public class GenresController : BaseController
{
    public GenresController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllGenres(CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var genres = await Mediator.Send(new GetAllGenresQuery(lang), cancellationToken);
        return Ok(genres);
    }
}
