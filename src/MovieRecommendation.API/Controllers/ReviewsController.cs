using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Reviews.Commands.CreateReply;
using MovieRecommendation.Application.Features.Reviews.Commands.DeleteReview;
using MovieRecommendation.Application.Features.Reviews.Commands.ToggleHelpful;
using MovieRecommendation.Application.Features.Reviews.Commands.UpdateReview;
using MovieRecommendation.Application.Features.Reviews.Queries.GetReviewReplies;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.API.Controllers;

public class ReviewsController : BaseController
{
    public ReviewsController(IMediator mediator) : base(mediator)
    {
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateReview(Guid id, UpdateMovieReviewDto request, CancellationToken ct)
    {
        await Mediator.Send(new UpdateReviewCommand(id, User.GetId(), request), ct);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken ct)
    {
        var isModerator = User.IsInRole(nameof(RoleType.Admin)) || User.IsInRole(nameof(RoleType.Moderator));
        await Mediator.Send(new DeleteReviewCommand(id, User.GetId(), isModerator), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:guid}/helpful")]
    public async Task<IActionResult> ToggleHelpful(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new ToggleHelpfulCommand(id, User.GetId()), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetReplies(Guid id, CancellationToken ct)
    {
        var viewerId = User.GetIdOrDefault() ?? Guid.Empty;
        var replies = await Mediator.Send(new GetReviewRepliesQuery(id, viewerId), ct);
        return Ok(replies);
    }

    [Authorize]
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> CreateReply(Guid id, CreateReplyDto request, CancellationToken ct)
    {
        var reply = await Mediator.Send(new CreateReplyCommand(id, User.GetId(), request), ct);
        return Ok(reply);
    }
}
