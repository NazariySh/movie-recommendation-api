using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MovieRecommendation.Application.DTOs;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Infrastructure.Services;

public class JwtTokenProvider : ITokenProvider
{
    public const int RefreshTokenLength = 64;

    private const string Algorithm = SecurityAlgorithms.HmacSha256;

    private readonly JwtSettings _jwtSettings;

    public JwtTokenProvider(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        var tokenDescriptor = GetTokenDescriptor(GetTokenClaims(user, roles));

        return new JsonWebTokenHandler().CreateToken(tokenDescriptor);
    }

    public RefreshTokenDto GenerateRefreshToken()
    {
        string token = GenerateRandomToken();
        var expiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays);

        return new RefreshTokenDto(token, expiryTime);
    }

    public async Task<ClaimsPrincipal> GetPrincipalAsync(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            accessToken,
            GetTokenValidationParameters());

        if (!result.IsValid)
        {
            throw new SecurityTokenException("Invalid token");
        }

        return new ClaimsPrincipal(result.ClaimsIdentity);
    }

    private static string GenerateRandomToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(RefreshTokenLength);
        return Convert.ToBase64String(randomBytes);
    }

    private SecurityTokenDescriptor GetTokenDescriptor(IEnumerable<Claim> claims)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

        return new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryInMinutes),
            SigningCredentials = new SigningCredentials(securityKey, Algorithm),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
        };
    }

    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key))
        };
    }

    private static List<Claim> GetTokenClaims(User user, IEnumerable<string> roles)
    {
        ArgumentException.ThrowIfNullOrEmpty(user.UserName);

        var tokenClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
        };

        tokenClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return tokenClaims;
    }
}
