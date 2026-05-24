using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Auth.Commands.ForgotPassword;
using MovieRecommendation.Application.Features.Auth.Commands.GoogleAuth;
using MovieRecommendation.Application.Features.Auth.Commands.Login;
using MovieRecommendation.Application.Features.Auth.Commands.Logout;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Features.Auth.Commands.Register;
using MovieRecommendation.Application.Features.Auth.Commands.ResendVerification;
using MovieRecommendation.Application.Features.Auth.Commands.ResetPassword;
using MovieRecommendation.Application.Features.Auth.Commands.VerifyEmail;
using MovieRecommendation.Application.Features.Auth.Queries.GetCurrentUser;
using MovieRecommendation.Application.Features.Users.Commands.ChangePassword;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.API.Controllers;

public class AuthController : BaseController
{
    public AuthController(IMediator mediator)
        : base(mediator)
    {
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new RegisterCommand(request), ct);

        return Ok(new { message = "Registration successful. Check your email to verify your account." });
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new VerifyEmailCommand(request), ct);
        return Ok();
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new ResendVerificationCommand(request), ct);
        return Ok();
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken ct)
    {
        var result = await Mediator.Send(new LoginCommand(request), ct);
        return Ok(result);
    }

    [HttpPost("google")]
    [EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Google(GoogleAuthRequestDto request, CancellationToken ct)
    {
        var result = await Mediator.Send(new GoogleAuthCommand(request), ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await Mediator.Send(new LogoutCommand(User.GetId()), ct);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new ForgotPasswordCommand(request), ct);
        return Ok();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-forgot")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new ResetPasswordCommand(request), ct);
        return Ok();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new ChangePasswordCommand(User.GetId(), request), ct);
        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var user = await Mediator.Send(new GetCurrentUserQuery(User.GetId()), ct);
        return Ok(user);
    }

    [Authorize(Roles = RoleTypes.Admin)]
    [HttpPost("register/{roleName}")]
    public async Task<IActionResult> RegisterWithRole(RoleType roleName, RegisterRequestDto request, CancellationToken ct)
    {
        await Mediator.Send(new RegisterCommand(request, roleName), ct);
        return Ok();
    }

    [HttpGet("auth-state")]
    public IActionResult GetAuthState()
    {
        return Ok(new { isAuthenticated = User.IsAuthenticated() });
    }
}
