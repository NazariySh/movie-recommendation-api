using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Surveys;
using MovieRecommendation.Application.Features.Surveys.Commands.SubmitSurveyResponse;
using MovieRecommendation.Application.Features.Surveys.Queries.GetCurrentSurvey;
using MovieRecommendation.Application.Features.Surveys.Queries.GetMyLatestResponse;

namespace MovieRecommendation.API.Controllers;

public class SurveysController : BaseController
{
    public SurveysController(IMediator mediator) : base(mediator)
    {
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var survey = await Mediator.Send(new GetCurrentSurveyQuery(lang), ct);
        return Ok(survey);
    }

    [Authorize]
    [HttpPost("responses")]
    public async Task<IActionResult> Submit(SubmitSurveyDto request, CancellationToken ct)
    {
        await Mediator.Send(new SubmitSurveyResponseCommand(User.GetId(), request), ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("my-response")]
    public async Task<IActionResult> GetMyLatestResponse(CancellationToken ct)
    {
        var response = await Mediator.Send(new GetMyLatestSurveyResponseQuery(User.GetId()), ct);
        return response is null ? NoContent() : Ok(response);
    }
}
