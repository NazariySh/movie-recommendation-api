using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string PreferredLanguage { get; set; } = LanguageCodes.Default;
}
