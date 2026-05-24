using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Recommendations;
using MovieRecommendation.Application.Interfaces.ML;

namespace MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;

public class TriggerRetrainCommandHandler : ICommandHandler<TriggerRetrainCommand, RetrainResultDto>
{
    private readonly IModelRetrainingOrchestrator _orchestrator;

    public TriggerRetrainCommandHandler(IModelRetrainingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<RetrainResultDto> Handle(TriggerRetrainCommand request, CancellationToken cancellationToken)
    {
        return request.WaitForCompletion
            ? _orchestrator.RunAsync(cancellationToken)
            : Task.FromResult(_orchestrator.Enqueue());
    }
}
