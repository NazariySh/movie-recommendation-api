namespace MovieRecommendation.Application.DTOs.Users;

public class UserProfileDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool EmailConfirmed { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public string PreferredLanguage { get; set; }

    public bool OnboardingCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<string> Roles { get; set; } = [];
}
