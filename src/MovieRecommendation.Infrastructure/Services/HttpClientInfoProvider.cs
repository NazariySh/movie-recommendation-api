using Microsoft.AspNetCore.Http;
using MovieRecommendation.Application.Interfaces;

namespace MovieRecommendation.Infrastructure.Services;

public class HttpClientInfoProvider : IClientInfoProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientInfoProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetIpAddress()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null)
        {
            return null;
        }

        if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            return forwarded.ToString().Split(',').FirstOrDefault()?.Trim();
        }

        return ctx.Connection.RemoteIpAddress?.ToString();
    }
}
