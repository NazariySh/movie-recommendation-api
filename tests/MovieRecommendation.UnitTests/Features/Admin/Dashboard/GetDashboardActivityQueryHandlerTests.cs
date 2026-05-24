using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardActivity;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Admin.Dashboard;

public class GetDashboardActivityQueryHandlerTests
{
    private readonly Mock<IAdminDashboardRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetDashboardActivityQueryHandler _handler;

    public GetDashboardActivityQueryHandlerTests()
    {
        _repositoryMock = new Mock<IAdminDashboardRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetDashboardActivityQueryHandler(_repositoryMock.Object, _cacheMock.Object);
    }

    [Theory]
    [InlineData(0, DashboardWindows.ActivityDaysDefault)]
    [InlineData(-5, DashboardWindows.ActivityDaysDefault)]
    [InlineData(7, 7)]
    [InlineData(1000, DashboardWindows.ActivityDaysMax)]
    public async Task Handle_Should_ClampDaysToAllowedRange(int requested, int expected)
    {
        var activity = new DashboardActivityDto();
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<DashboardActivityDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<DashboardActivityDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));
        _repositoryMock
            .Setup(r => r.GetActivityAsync(expected, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        var result = await _handler.Handle(new GetDashboardActivityQuery(requested), CancellationToken.None);

        result.Should().BeSameAs(activity);
        _repositoryMock.Verify(r => r.GetActivityAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_BuildCacheKey_WithDaysSuffix()
    {
        var capturedKey = string.Empty;
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<DashboardActivityDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<CancellationToken, Task<DashboardActivityDto>>, TimeSpan, CancellationToken>(
                (key, _, _, _) => capturedKey = key)
            .ReturnsAsync(new DashboardActivityDto());

        await _handler.Handle(new GetDashboardActivityQuery(14), CancellationToken.None);

        capturedKey.Should().Be($"{AdminCacheKeys.DashboardActivityPrefix}14");
    }
}
