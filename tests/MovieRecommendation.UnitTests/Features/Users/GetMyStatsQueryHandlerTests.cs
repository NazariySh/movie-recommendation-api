using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Features.Users.Queries.GetMyStats;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Users;

public class GetMyStatsQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetMyStatsQueryHandler _handler;

    public GetMyStatsQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetMyStatsQueryHandler(_userRepositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnCached_When_Hit()
    {
        var userId = Guid.NewGuid();
        var cached = new UserStatsDto { TotalRatings = 42 };
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                UserCacheKeys.Stats(userId, "uk"),
                It.IsAny<Func<CancellationToken, Task<UserStatsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _handler.Handle(new GetMyStatsQuery(userId, "uk"), CancellationToken.None);

        result.Should().BeSameAs(cached);
        _userRepositoryMock.Verify(r => r.GetStatsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_InvokeRepository_OnCacheMiss()
    {
        var userId = Guid.NewGuid();
        var stats = new UserStatsDto { TotalRatings = 7 };
        _userRepositoryMock
            .Setup(r => r.GetStatsAsync(userId, "uk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<UserStatsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<UserStatsDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var result = await _handler.Handle(new GetMyStatsQuery(userId, "uk"), CancellationToken.None);

        result.Should().BeSameAs(stats);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_RepositoryReturnsNull()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(r => r.GetStatsAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStatsDto?)null);
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<UserStatsDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<UserStatsDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var act = () => _handler.Handle(new GetMyStatsQuery(userId, "uk"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
