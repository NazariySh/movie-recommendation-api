using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;
using MovieRecommendation.Application.Features.Search.Queries.GetSearchCounts;
using MovieRecommendation.Application.Features.Search.Queries.GetSearchSuggestions;

namespace MovieRecommendation.API.Controllers;

public class SearchController : BaseController
{
    public SearchController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var result = await Mediator.Send(new GetSearchSuggestionsQuery(q ?? string.Empty, lang, limit), ct);
        return Ok(result);
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetCounts([FromQuery] string q, CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var counts = await Mediator.Send(new GetSearchCountsQuery(q ?? string.Empty, lang), ct);
        return Ok(counts);
    }

    [HttpGet("semantic")]
    public async Task<IActionResult> SemanticSearch(
        [FromQuery] string q,
        [FromQuery] int limit = 20,
        [FromQuery] double minScore = 0.60,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var result = await Mediator.Send(
            new SemanticSearchMoviesQuery(q ?? string.Empty, lang, limit, minScore),
            ct);
        return Ok(result);
    }
}
