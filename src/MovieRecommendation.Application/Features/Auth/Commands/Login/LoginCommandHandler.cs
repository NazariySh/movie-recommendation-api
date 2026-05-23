using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly ICookieService _cookieService;
    private readonly IMapper _mapper;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        ITokenProvider tokenProvider,
        ICookieService cookieService,
        IMapper mapper,
        ILogger<LoginCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _tokenProvider = tokenProvider;
        _cookieService = cookieService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Model.Email);

        if (user is null || user.IsDeleted)
        {
            _logger.LogWarning("Login failed: user not found ({Email})", request.Model.Email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login locked out for {UserId}", user.Id);
            throw new UnauthorizedAccessException("Account is locked. Try again later.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Model.Password))
        {
            await _userManager.AccessFailedAsync(user);

            _logger.LogWarning("Login failed: bad password for {UserId}", user.Id);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var userRoles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenProvider.GenerateAccessToken(user, userRoles);
        var refreshTokenDto = _tokenProvider.GenerateRefreshToken();

        var refreshTokenEntity = new Domain.Entities.Users.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenDto.Token,
            ExpiresAt = refreshTokenDto.ExpiryTime,
        };

        _refreshTokenRepository.Add(refreshTokenEntity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cookieService.SetCookie(
            RefreshTokenCommandHandler.RefreshTokenCookieName,
            refreshTokenDto.Token,
            refreshTokenDto.ExpiryTime);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        var authUser = _mapper.Map<AuthUserDto>(user);
        authUser.Roles = userRoles.ToList();

        return new LoginResponseDto(accessToken, authUser);
    }
}
