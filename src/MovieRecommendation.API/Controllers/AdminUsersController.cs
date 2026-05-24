using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Features.Admin.Users.Commands.AdminDeleteUser;
using MovieRecommendation.Application.Features.Admin.Users.Commands.DisableUser;
using MovieRecommendation.Application.Features.Admin.Users.Commands.EnableUser;
using MovieRecommendation.Application.Features.Admin.Users.Commands.ForceResetPassword;
using MovieRecommendation.Application.Features.Admin.Users.Commands.UpdateUserRoles;
using MovieRecommendation.Application.Features.Admin.Users.Queries.GetAdminUserDetail;
using MovieRecommendation.Application.Features.Admin.Users.Queries.SearchAdminUsers;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleTypes.Admin)]
public class AdminUsersController : BaseController
{
    public AdminUsersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchAdminUsersDto query, CancellationToken ct)
    {
        var result = await Mediator.Send(new SearchAdminUsersQuery(query), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var detail = await Mediator.Send(new GetAdminUserDetailQuery(id), ct);
        return Ok(detail);
    }

    [HttpPatch("{id:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(Guid id, UpdateUserRolesDto body, CancellationToken ct)
    {
        await Mediator.Send(new UpdateUserRolesCommand(id, body.Roles), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, DisableUserDto body, CancellationToken ct)
    {
        await Mediator.Send(new DisableUserCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new EnableUserCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/force-reset-password")]
    public async Task<IActionResult> ForceResetPassword(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new ForceResetPasswordCommand(id), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new AdminDeleteUserCommand(id), ct);
        return NoContent();
    }
}
