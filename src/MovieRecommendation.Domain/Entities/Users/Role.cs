using Microsoft.AspNetCore.Identity;

namespace MovieRecommendation.Domain.Entities.Users;

public class Role : IdentityRole<Guid>, IAuditable
{
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
