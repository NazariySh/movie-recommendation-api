using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Ratings.Queries.GetUserRatings;
using MovieRecommendation.Application.Features.Users.Commands.ChangePassword;
using MovieRecommendation.Application.Features.Users.Commands.DeleteUser;
using MovieRecommendation.Application.Features.Users.Commands.UpdateGenrePreferences;
using MovieRecommendation.Application.Features.Users.Commands.UpdateProfile;
using MovieRecommendation.Application.Features.Users.Commands.UploadAvatar;
using MovieRecommendation.Application.Features.Users.Queries.GetMyGenrePreferences;
using MovieRecommendation.Application.Features.Users.Queries.GetMyProfile;
using MovieRecommendation.Application.Features.Users.Queries.GetMyStats;
using MovieRecommendation.Application.Features.Users.Queries.GetPublicProfile;

namespace MovieRecommendation.API.Controllers;

public class UsersController : BaseController
{
    public UsersController(IMediator mediator)
        : base(mediator)
    {
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var profile = await Mediator.Send(new GetMyProfileQuery(User.GetId()), ct);
        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto request, CancellationToken ct)
    {
        var profile = await Mediator.Send(new UpdateProfileCommand(User.GetId(), request), ct);
        return Ok(profile);
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        await Mediator.Send(new DeleteUserCommand(User.GetId()), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("me/avatar")]
    [EnableRateLimiting("auth-forgot")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        await using var stream = file.OpenReadStream();
        var profile = await Mediator.Send(
            new UploadAvatarCommand(
                UserId: User.GetId(),
                Content: stream,
                ContentType: file.ContentType,
                Length: file.Length),
            ct);

        return Ok(profile);
    }

    [Authorize]
    [HttpGet("me/stats")]
    public async Task<IActionResult> GetMyStats(CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var stats = await Mediator.Send(new GetMyStatsQuery(User.GetId(), lang), ct);
        return Ok(stats);
    }

    [Authorize]
    [HttpGet("me/genre-preferences")]
    public async Task<IActionResult> GetMyGenrePreferences(CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var prefs = await Mediator.Send(new GetMyGenrePreferencesQuery(User.GetId(), lang), ct);
        return Ok(prefs);
    }

    [Authorize]
    [HttpPut("me/genre-preferences")]
    public async Task<IActionResult> UpdateMyGenrePreferences(
        IReadOnlyList<UpdateGenrePreferenceDto> request,
        CancellationToken ct)
    {
        var lang = HttpContext.GetRequestLanguage();
        var prefs = await Mediator.Send(new UpdateGenrePreferencesCommand(User.GetId(), request, lang), ct);
        return Ok(prefs);
    }

    [Authorize]
    [HttpPatch("me/password")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new ChangePasswordCommand(User.GetId(), request), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/public")]
    public async Task<IActionResult> GetPublicProfile(Guid id, CancellationToken ct)
    {
        var profile = await Mediator.Send(new GetPublicProfileQuery(id), ct);
        return Ok(profile);
    }

    [HttpGet("{userId:guid}/ratings")]
    public async Task<IActionResult> GetUserRatings(
        Guid userId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var lang = HttpContext.GetRequestLanguage();
        var page = await Mediator.Send(new GetUserRatingsQuery(userId, pageNumber, pageSize, lang), ct);
        return Ok(page);
    }
}
