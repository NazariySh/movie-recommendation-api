using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IMediator Mediator { get; }

    protected BaseController(IMediator mediator)
    {
        Mediator = mediator;
    }
}
