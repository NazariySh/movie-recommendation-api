using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.Common.Models;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IMediator Mediator { get; }

    protected BaseController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected static Stream? OpenUpload(IFormFile? file)
        => file is { Length: > 0 } ? file.OpenReadStream() : null;

    protected static ImageUpload? AsImageUpload(IFormFile? file, Stream? stream)
        => file is { Length: > 0 } && stream is not null
            ? new ImageUpload(stream, file.ContentType)
            : null;
}
