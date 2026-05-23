using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.Application.DTOs.Users;

public class UpdateProfileDto
{
    public string Username { get; set; } = null!;

    public string? Bio { get; set; }

    public string PreferredLanguage { get; set; } = LanguageCodes.Default;
}
