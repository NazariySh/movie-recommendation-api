using FluentAssertions;
using Moq;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Users.Queries.GetPublicProfile;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Users;

public class GetPublicProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetPublicProfileQueryHandler _handler;

    public GetPublicProfileQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetPublicProfileQueryHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_RepositoryReturnsNull()
    {
        var id = Guid.NewGuid();
        _userRepositoryMock
            .Setup(r => r.GetPublicProfileAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublicProfileDto?)null);

        var act = () => _handler.Handle(new GetPublicProfileQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnProfile_When_Found()
    {
        var id = Guid.NewGuid();
        var profile = new PublicProfileDto { Id = id, Username = "alice" };
        _userRepositoryMock
            .Setup(r => r.GetPublicProfileAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _handler.Handle(new GetPublicProfileQuery(id), CancellationToken.None);

        result.Should().BeSameAs(profile);
    }
}
