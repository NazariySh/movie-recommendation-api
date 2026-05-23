using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.UnitTests.Infrastructure;

internal static class UserManagerMockFactory
{
    public static Mock<UserManager<User>> Create()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
