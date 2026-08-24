namespace MovieRecommendation.Application.DTOs.Admin;

public class AdminUserDetailDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public string PreferredLanguage { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public AdminUserActivityDto Activity { get; set; } = new();
}
