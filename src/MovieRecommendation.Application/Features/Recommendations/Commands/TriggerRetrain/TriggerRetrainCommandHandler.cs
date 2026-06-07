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

    public async Task<RetrainResultDto> Handle(TriggerRetrainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Actor {ActorUserId} triggered ML retrain (wait={Wait})",
            request.ActorUserId,
            request.WaitForCompletion);

        if (request.WaitForCompletion)
        {
            var result = await _orchestrator.RunAsync(request.ActorUserId, cancellationToken);

            _logger.LogInformation(
                "ML retrain completed for Actor {ActorUserId} with JobId {JobId} and Status {Status}",
                request.ActorUserId,
                result.JobId,
                result.Status);

            return result;
        }
        else
        {
            var result = _orchestrator.Enqueue(request.ActorUserId);

            _logger.LogInformation(
                "ML retrain enqueued for Actor {ActorUserId} with JobId {JobId}",
                request.ActorUserId,
                result.JobId);

            return result;
        }
    }
}
