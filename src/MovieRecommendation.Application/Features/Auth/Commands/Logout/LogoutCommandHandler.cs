using MediatR;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICookieService _cookieService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ICookieService cookieService,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _cookieService = cookieService;
        _logger = logger;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = _cookieService.GetCookie(RefreshTokenCommandHandler.RefreshTokenCookieName);

        if (!string.IsNullOrEmpty(token))
        {
            var stored = await _refreshTokenRepository.FindByTokenAsync(token, cancellationToken);

            if (stored is not null && !stored.IsRevoked)
            {
                stored.RevokedAt = DateTime.UtcNow;
                _refreshTokenRepository.Update(stored);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        _cookieService.RemoveCookie(RefreshTokenCommandHandler.RefreshTokenCookieName);
        _logger.LogInformation("User {UserId} logged out", request.UserId);

        return Unit.Value;
    }
}
