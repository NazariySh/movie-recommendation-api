using FluentAssertions;
using Moq;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Features.Admin.Users.Queries.SearchAdminUsers;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.UnitTests.Features.Admin.Users;

public class SearchAdminUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly SearchAdminUsersQueryHandler _handler;

    public SearchAdminUsersQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new SearchAdminUsersQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_DelegateToRepository()
    {
        var dto = new SearchAdminUsersDto { PageNumber = 2, PageSize = 25, Search = "alice" };
        var paged = new PagedList<AdminUserListItemDto>(
            new List<AdminUserListItemDto> { new() { Id = Guid.NewGuid(), Username = "alice" } },
            2, 25, 1);
        _userRepositoryMock
            .Setup(r => r.SearchAdminAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _handler.Handle(new SearchAdminUsersQuery(dto), CancellationToken.None);

        result.Should().BeSameAs(paged);
        _userRepositoryMock.Verify(r => r.SearchAdminAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }
}
