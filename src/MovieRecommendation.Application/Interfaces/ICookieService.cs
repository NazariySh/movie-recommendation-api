namespace MovieRecommendation.Application.Interfaces;

public interface ICookieService
{
    string? GetCookie(string key);

    void SetCookie(string key, string value, DateTime expiryTime);

    void RemoveCookie(string key);
}
