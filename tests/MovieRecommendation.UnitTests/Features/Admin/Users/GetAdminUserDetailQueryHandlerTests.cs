using FluentAssertions;
using Moq;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Features.Admin.Users.Queries.GetAdminUserDetail;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class GetAdminUserDetailQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetAdminUserDetailQueryHandler _handler;

    public GetAdminUserDetailQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetAdminUserDetailQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_DetailMissing()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(r => r.GetAdminDetailAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUserDetailDto?)null);

        var act = () => _handler.Handle(new GetAdminUserDetailQuery(userId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnDetail_When_Found()
    {
        var userId = Guid.NewGuid();
        var detail = new AdminUserDetailDto { Id = userId, Username = "alice", Email = "a@x.com" };
        _userRepositoryMock
            .Setup(r => r.GetAdminDetailAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _handler.Handle(new GetAdminUserDetailQuery(userId), CancellationToken.None);

        result.Should().BeSameAs(detail);
    }
}
