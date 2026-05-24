using MovieRecommendation.Application.DTOs.Recommendations;

namespace MovieRecommendation.Application.Interfaces.ML;

public interface IModelRetrainingOrchestrator
{
    Task<RetrainResultDto> RunAsync(Guid? actorUserId = null, CancellationToken cancellationToken = default);

    RetrainResultDto Enqueue(Guid? actorUserId = null);
}
