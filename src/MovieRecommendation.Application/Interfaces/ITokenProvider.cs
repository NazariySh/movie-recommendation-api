using System.Security.Claims;
using MovieRecommendation.Application.DTOs;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Interfaces;

public interface ITokenProvider
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);

    RefreshTokenDto GenerateRefreshToken();

    Task<ClaimsPrincipal> GetPrincipalAsync(string accessToken);
}
