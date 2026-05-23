using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Application.DTOs.Auth;
using MovieRecommendation.Application.Features.Auth.Commands.RefreshToken;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Models;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Auth.Commands.GoogleAuth;

public class GoogleAuthCommandHandler : ICommandHandler<GoogleAuthCommand, LoginResponseDto>
{
    private readonly IGoogleAuthValidator _googleValidator;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly ICookieService _cookieService;
    private readonly IMapper _mapper;
    private readonly ILogger<GoogleAuthCommandHandler> _logger;

    public GoogleAuthCommandHandler(
        IGoogleAuthValidator googleValidator,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        ITokenProvider tokenProvider,
        ICookieService cookieService,
        IMapper mapper,
        ILogger<GoogleAuthCommandHandler> logger)
    {
        _googleValidator = googleValidator;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _tokenProvider = tokenProvider;
        _cookieService = cookieService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LoginResponseDto> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
    {
        var payload = await _googleValidator.ValidateAsync(request.Model.IdToken, cancellationToken);
        if (!payload.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google email is not verified");
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);

        user ??= await CreateUserFromGoogle(payload, cancellationToken);

        if (user.IsDeleted)
        {
            throw new UnauthorizedAccessException("Account is disabled");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenProvider.GenerateAccessToken(user, roles);
        var refreshDto = _tokenProvider.GenerateRefreshToken();

        _refreshTokenRepository.Add(new Domain.Entities.Users.RefreshToken
        {
            UserId = user.Id,
            Token = refreshDto.Token,
            ExpiresAt = refreshDto.ExpiryTime,
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cookieService.SetCookie(
            RefreshTokenCommandHandler.RefreshTokenCookieName,
            refreshDto.Token,
            refreshDto.ExpiryTime);

        var authUser = _mapper.Map<AuthUserDto>(user);
        authUser.Roles = roles.ToList();

        return new LoginResponseDto(accessToken, authUser);
    }

    private async Task<User> CreateUserFromGoogle(GooglePayload payload, CancellationToken ct)
    {
        var username = await GenerateUniqueUsernameAsync(payload.Email, ct);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var user = new User
            {
                UserName = username,
                Email = payload.Email,
                EmailConfirmed = true,
                AvatarUrl = payload.Picture,
            };

            var createResult = await _userManager.CreateAsync(user);
            createResult.EnsureSucceeded("Google user creation failed");

            var roleResult = await _userManager.AddToRoleAsync(user, RoleType.User.ToString());
            roleResult.EnsureSucceeded("Adding Google user to role failed");

            await transaction.CommitAsync(ct);

            _logger.LogInformation("Created new Google user {UserId} ({Email})", user.Id, user.Email);

            return user;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private const int MinUsernameLength = 3;
    private const int MaxUsernameAttempts = 20;

    private async Task<string> GenerateUniqueUsernameAsync(string email, CancellationToken ct)
    {
        var baseName = email.Split('@')[0];
        baseName = new string(baseName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (baseName.Length < MinUsernameLength)
        {
            baseName = "user" + baseName;
        }

        var candidate = baseName;
        for (var i = 1; i <= MaxUsernameAttempts; i++)
        {
            if (await _userRepository.IsUsernameUniqueAsync(candidate, ct))
            {
                return candidate;
            }

            candidate = $"{baseName}{i}";
        }

        return $"{baseName}{Guid.NewGuid().ToString("N").AsSpan(0, 6)}";
    }
}
