using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.UnitTests.Features.Admin.Dashboard;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly Mock<IAdminDashboardRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetDashboardSummaryQueryHandler _handler;

    public GetDashboardSummaryQueryHandlerTests()
    {
        _repositoryMock = new Mock<IAdminDashboardRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetDashboardSummaryQueryHandler(_repositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnCachedValue()
    {
        var cached = new DashboardSummaryDto { TotalUsers = 42, TotalMovies = 100 };
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                AdminCacheKeys.DashboardSummary,
                It.IsAny<Func<CancellationToken, Task<DashboardSummaryDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Should().BeSameAs(cached);
        _repositoryMock.Verify(r => r.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_InvokeRepository_OnCacheMiss()
    {
        var summary = new DashboardSummaryDto { TotalUsers = 7 };
        _repositoryMock.Setup(r => r.GetSummaryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(summary);
        _cacheMock
            .Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<DashboardSummaryDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<DashboardSummaryDto>>, TimeSpan, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Should().BeSameAs(summary);
        _repositoryMock.Verify(r => r.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
