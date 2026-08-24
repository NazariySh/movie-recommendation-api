using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.Features.Movies.Queries.GetTrendingMovies;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetBecauseYouLiked;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetColdStart;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetForYou;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetPopular;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : BaseController
{
    public RecommendationsController(IMediator mediator) : base(mediator)
    {
    }

    [Authorize]
    [HttpGet("for-you")]
    public async Task<IActionResult> GetForYou([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(new GetForYouQuery(User.GetId(), lang, count), ct);
        return Ok(movies);
    }

    [Authorize]
    [HttpGet("cold-start")]
    public async Task<IActionResult> GetColdStart([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(new GetColdStartQuery(User.GetId(), lang, count), ct);
        return Ok(movies);
    }

    [HttpGet("trending/movies")]
    public async Task<IActionResult> GetTrendingMovies(
        [FromQuery] int days = 7,
        [FromQuery] int count = 20,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(
            new GetTrendingMoviesQuery(lang, TitleType.Movie, count, days),
            ct);
        return Ok(movies);
    }

    [HttpGet("trending/series")]
    public async Task<IActionResult> GetTrendingSeries(
        [FromQuery] int days = 7,
        [FromQuery] int count = 20,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(
            new GetTrendingMoviesQuery(lang, TitleType.Series, count, days),
            ct);
        return Ok(movies);
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular(
        [FromQuery] string? genre = null,
        [FromQuery] TitleType? type = null,
        [FromQuery] int count = 20,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(new GetPopularQuery(lang, type, genre, count), ct);
        return Ok(movies);
    }

    [HttpGet("because-you-liked/{movieId:guid}")]
    public async Task<IActionResult> GetBecauseYouLiked(
        Guid movieId,
        [FromQuery] int count = 12,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var userId = User.GetIdOrDefault() ?? Guid.Empty;
        var movies = await Mediator.Send(new GetBecauseYouLikedQuery(movieId, lang, userId, count), ct);
        return Ok(movies);
    }
}
