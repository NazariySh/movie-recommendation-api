namespace MovieRecommendation.Application.DTOs.Admin;

public class AdminUserListItemDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LockoutEnd { get; set; }
}
