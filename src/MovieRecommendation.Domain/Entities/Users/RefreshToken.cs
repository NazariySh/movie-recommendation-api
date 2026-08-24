namespace MovieRecommendation.Domain.Entities.Users;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsRevoked => RevokedAt is not null;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
