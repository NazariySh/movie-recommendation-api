using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetModelStatus;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Predictions;

namespace MovieRecommendation.UnitTests.Features.Recommendations;

public class GetModelStatusQueryHandlerTests
{
    private readonly Mock<IMlModelMetadataRepository> _repositoryMock;
    private readonly GetModelStatusQueryHandler _handler;

    public GetModelStatusQueryHandlerTests()
    {
        _repositoryMock = new Mock<IMlModelMetadataRepository>();
        _handler = new GetModelStatusQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnInactiveDto_When_NoActiveModel()
    {
        _repositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((MlModelMetadata?)null);

        var result = await _handler.Handle(new GetModelStatusQuery(), CancellationToken.None);

        result.IsActive.Should().BeFalse();
        result.Version.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_MapEveryFieldFromMetadata()
    {
        var meta = new MlModelMetadata
        {
            Version = "v20260101_120000",
            Rmse = 0.8123,
            R2 = 0.6543,
            SampleCount = 12345,
            TrainedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            IsActive = true,
        };
        _repositoryMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(meta);

        var result = await _handler.Handle(new GetModelStatusQuery(), CancellationToken.None);

        result.IsActive.Should().BeTrue();
        result.Version.Should().Be(meta.Version);
        result.Rmse.Should().Be(meta.Rmse);
        result.R2.Should().Be(meta.R2);
        result.SampleCount.Should().Be(meta.SampleCount);
        result.TrainedAt.Should().Be(meta.TrainedAt);
    }
}
