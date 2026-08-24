using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Movies.Queries.GetAllMovies;
using MovieRecommendation.Application.Features.Movies.Queries.GetMovieById;
using MovieRecommendation.Application.Features.Movies.Queries.GetMovieByImdbId;
using MovieRecommendation.Application.Features.Movies.Queries.GetMovieByKey;
using MovieRecommendation.Application.Features.Movies.Queries.GetSimilarMovies;
using MovieRecommendation.Application.Features.Movies.Queries.GetTrendingMovies;
using MovieRecommendation.Application.Features.Movies.Queries.GetRecommendations;
using MovieRecommendation.Application.Features.Ratings.Commands.DeleteRating;
using MovieRecommendation.Application.Features.Ratings.Commands.UpsertRating;
using MovieRecommendation.Application.Features.Ratings.Queries.GetMyRating;
using MovieRecommendation.Application.Features.Reviews.Commands.CreateReview;
using MovieRecommendation.Application.Features.Reviews.Queries.GetMovieReviews;

namespace MovieRecommendation.API.Controllers;

public class MoviesController : BaseController
{
    public MoviesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMovies([FromQuery] SearchMoviesDto searchDto, CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(new GetAllMoviesQuery(searchDto, lang), cancellationToken);
        return Ok(movies);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingMovies(CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(new GetTrendingMoviesQuery(lang), cancellationToken);
        return Ok(movies);
    }

    [HttpGet("recommended")]
    public async Task<IActionResult> GetRecommendedMovies(CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var userId = User.GetIdOrDefault() ?? Guid.Empty;
        var movies = await Mediator.Send(new GetRecommendationsQuery(userId, 20, lang), cancellationToken);
        return Ok(movies);
    }

    [HttpGet("by-imdb/{imdbId}")]
    public async Task<IActionResult> GetByImdbId(string imdbId, CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movie = await Mediator.Send(new GetMovieByImdbIdQuery(imdbId, lang), cancellationToken);
        return Ok(movie);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMovieById(Guid id, CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movie = await Mediator.Send(new GetMovieByIdQuery(id, lang), cancellationToken);
        return Ok(movie);
    }

    [HttpGet("{id:guid}/similar")]
    public async Task<IActionResult> GetSimilar(Guid id, [FromQuery] int count = 12, CancellationToken cancellationToken = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movies = await Mediator.Send(new GetSimilarMoviesQuery(id, count, lang), cancellationToken);
        return Ok(movies);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetMovieByKey(string key, CancellationToken cancellationToken)
    {
        var lang = HttpContext.GetRequestLanguage();
        var movie = await Mediator.Send(new GetMovieByKeyQuery(key, lang), cancellationToken);
        return Ok(movie);
    }


    [Authorize]
    [HttpPut("{id:guid}/rating")]
    public async Task<IActionResult> UpsertRating(Guid id, UpsertRatingDto request, CancellationToken ct)
    {
        var rating = await Mediator.Send(new UpsertRatingCommand(User.GetId(), id, request), ct);
        return Ok(rating);
    }

    [Authorize]
    [HttpDelete("{id:guid}/rating")]
    public async Task<IActionResult> DeleteRating(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteRatingCommand(User.GetId(), id), ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("{id:guid}/rating/me")]
    public async Task<IActionResult> GetMyRating(Guid id, CancellationToken ct)
    {
        var rating = await Mediator.Send(new GetMyRatingQuery(User.GetId(), id), ct);
        return rating is null ? NoContent() : Ok(rating);
    }


    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetMovieReviews(
        Guid id,
        [FromQuery] SearchReviewsDto query,
        CancellationToken ct)
    {
        var viewerId = User.GetIdOrDefault() ?? Guid.Empty;
        var reviews = await Mediator.Send(new GetMovieReviewsQuery(id, query, viewerId), ct);
        return Ok(reviews);
    }

    [Authorize]
    [HttpPost("{id:guid}/reviews")]
    public async Task<IActionResult> CreateReview(Guid id, CreateMovieReviewDto request, CancellationToken ct)
    {
        var review = await Mediator.Send(new CreateReviewCommand(User.GetId(), id, request), ct);
        return CreatedAtAction(
            actionName: nameof(GetMovieById),
            controllerName: "Movies",
            routeValues: new { id },
            value: review);
    }
}
