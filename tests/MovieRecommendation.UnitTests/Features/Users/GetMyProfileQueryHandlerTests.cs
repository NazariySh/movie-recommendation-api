using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using MovieRecommendation.Application.Features.Users.Queries.GetMyProfile;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Users;

public class GetMyProfileQueryHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly IMapper _mapper;
    private readonly GetMyProfileQueryHandler _handler;

    public GetMyProfileQueryHandlerTests()
    {
        _userManagerMock = UserManagerMockFactory.Create();
        _mapper = TestMapperFactory.Create();
        _handler = new GetMyProfileQueryHandler(_userManagerMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_UserMissing()
    {
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new GetMyProfileQuery(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ProjectUserFields_And_Roles()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "alice",
            Email = "alice@example.com",
            EmailConfirmed = true,
            AvatarUrl = "a.png",
            Bio = "hi",
            PreferredLanguage = "uk",
            OnboardingCompleted = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        _userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User", "Moderator" });

        var dto = await _handler.Handle(new GetMyProfileQuery(user.Id), CancellationToken.None);

        dto.Id.Should().Be(user.Id);
        dto.Username.Should().Be("alice");
        dto.Email.Should().Be("alice@example.com");
        dto.Roles.Should().BeEquivalentTo(new[] { "User", "Moderator" });
    }
}
