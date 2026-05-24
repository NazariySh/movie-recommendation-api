using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieRecommendation.Application.DTOs.Recommendations;
using MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;
using MovieRecommendation.Application.Interfaces.ML;

namespace MovieRecommendation.UnitTests.Features.Recommendations;

public class TriggerRetrainCommandHandlerTests
{
    private readonly Mock<IModelRetrainingOrchestrator> _orchestratorMock;
    private readonly TriggerRetrainCommandHandler _handler;

    public TriggerRetrainCommandHandlerTests()
    {
        _orchestratorMock = new Mock<IModelRetrainingOrchestrator>();
        _handler = new TriggerRetrainCommandHandler(
            _orchestratorMock.Object,
            NullLogger<TriggerRetrainCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_CallRunAsync_When_WaitForCompletion()
    {
        var actor = Guid.NewGuid();
        var expected = new RetrainResultDto { JobId = "retrain-x", Status = "Completed", TriggeredAt = DateTime.UtcNow };
        _orchestratorMock
            .Setup(o => o.RunAsync(actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(
            new TriggerRetrainCommand(actor, WaitForCompletion: true),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        _orchestratorMock.Verify(o => o.RunAsync(actor, It.IsAny<CancellationToken>()), Times.Once);
        _orchestratorMock.Verify(o => o.Enqueue(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_CallEnqueue_When_NotWaiting()
    {
        var actor = Guid.NewGuid();
        var expected = new RetrainResultDto { JobId = "retrain-y", Status = "Started", TriggeredAt = DateTime.UtcNow };
        _orchestratorMock
            .Setup(o => o.Enqueue(actor))
            .Returns(expected);

        var result = await _handler.Handle(
            new TriggerRetrainCommand(actor),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        _orchestratorMock.Verify(o => o.Enqueue(actor), Times.Once);
        _orchestratorMock.Verify(o => o.RunAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
