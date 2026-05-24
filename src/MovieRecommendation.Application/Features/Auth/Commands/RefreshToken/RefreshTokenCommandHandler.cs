using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, LoginResponseDto>
{
    public const string RefreshTokenCookieName = "refreshToken";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly ICookieService _cookieService;
    private readonly IMapper _mapper;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        ITokenProvider tokenProvider,
        ICookieService cookieService,
        IMapper mapper,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _tokenProvider = tokenProvider;
        _cookieService = cookieService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingToken = _cookieService.GetCookie(RefreshTokenCookieName);

        if (string.IsNullOrEmpty(incomingToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var stored = await _refreshTokenRepository.FindByTokenAsync(incomingToken, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (stored.IsRevoked)
        {
            _logger.LogError(
                "REFRESH TOKEN RE-USE DETECTED for user {UserId}. Revoking all tokens.",
                stored.UserId);

            await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId, cancellationToken);
            _cookieService.RemoveCookie(RefreshTokenCookieName);

            throw new UnauthorizedAccessException("Refresh token re-use detected. All sessions revoked.");
        }

        if (DateTime.UtcNow >= stored.ExpiresAt)
        {
            _cookieService.RemoveCookie(RefreshTokenCookieName);
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.IsDeleted)
        {
            throw new UnauthorizedAccessException("User account is disabled.");
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Refresh blocked: email not verified for {UserId}", user.Id);
            throw new EmailNotVerifiedException("Email is not verified. Check your inbox.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenProvider.GenerateAccessToken(user, roles);
        var newRefreshDto = _tokenProvider.GenerateRefreshToken();

        var newRefreshEntity = new Domain.Entities.Users.RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshDto.Token,
            ExpiresAt = newRefreshDto.ExpiryTime,
        };

        _refreshTokenRepository.Add(newRefreshEntity);

        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByToken = newRefreshDto.Token;

        _refreshTokenRepository.Update(stored);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cookieService.SetCookie(
            RefreshTokenCookieName,
            newRefreshDto.Token,
            newRefreshDto.ExpiryTime);

        var authUser = _mapper.Map<AuthUserDto>(user);
        authUser.Roles = roles.ToList();

        return new LoginResponseDto(newAccessToken, authUser);
    }
}
