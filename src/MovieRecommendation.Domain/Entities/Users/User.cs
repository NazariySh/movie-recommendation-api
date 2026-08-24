using Microsoft.AspNetCore.Identity;

namespace MovieRecommendation.Domain.Entities.Users;

public class User : IdentityUser<Guid>, IAuditable, ISoftDeletable
{
    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public string PreferredLanguage { get; set; } = null!;

    public bool OnboardingCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
