namespace MovieRecommendation.Application.DTOs.Auth;

public class AuthUserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool OnboardingCompleted { get; set; }

    public string PreferredLanguage { get; set; }

    public List<string> Roles { get; set; } = [];
}
