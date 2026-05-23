using Microsoft.AspNetCore.Http;
using MovieRecommendation.Application.Interfaces;

namespace MovieRecommendation.Infrastructure.Services;

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCookie(string key)
    {
        var cookies = _httpContextAccessor.HttpContext?.Request.Cookies;

        return cookies != null && cookies.TryGetValue(key, out var value) ? value : null;
    }

    public void SetCookie(string key, string value, DateTime expiryTime)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiryTime
        };

        _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, options);
    }

    public void RemoveCookie(string key)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key);
    }
}
