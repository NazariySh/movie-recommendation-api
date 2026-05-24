using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Recommendations;
using MovieRecommendation.Application.Interfaces.ML;

namespace MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;

public class TriggerRetrainCommandHandler : ICommandHandler<TriggerRetrainCommand, RetrainResultDto>
{
    private readonly IModelRetrainingOrchestrator _orchestrator;
    private readonly ILogger<TriggerRetrainCommandHandler> _logger;

    public TriggerRetrainCommandHandler(
        IModelRetrainingOrchestrator orchestrator,
        ILogger<TriggerRetrainCommandHandler> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public Task<RetrainResultDto> Handle(TriggerRetrainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Actor {ActorUserId} triggered ML retrain (wait={Wait})",
            request.ActorUserId,
            request.WaitForCompletion);

        return request.WaitForCompletion
            ? _orchestrator.RunAsync(request.ActorUserId, cancellationToken)
            : Task.FromResult(_orchestrator.Enqueue(request.ActorUserId));
    }
}
