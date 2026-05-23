using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Domain.Constants;

public static class RoleTypes
{
    public const string User = nameof(RoleType.User);
    public const string Moderator = nameof(RoleType.Moderator);
    public const string Admin = nameof(RoleType.Admin);
}
