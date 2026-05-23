using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Features.Users;

public static class UserAnonymizer
{
    public static void Anonymize(User user)
    {
        var suffix = Guid.NewGuid().ToString("N")[..UserAnonymization.SuffixLength];
        user.UserName = $"{UserAnonymization.UsernamePrefix}{suffix}";
        user.NormalizedUserName = user.UserName.ToUpperInvariant();
        user.Email = $"{suffix}{UserAnonymization.EmailDomain}";
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.AvatarUrl = null;
        user.Bio = null;
        user.EmailConfirmed = false;
        user.LockoutEnd = DateTimeOffset.MaxValue;
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
    }
}
