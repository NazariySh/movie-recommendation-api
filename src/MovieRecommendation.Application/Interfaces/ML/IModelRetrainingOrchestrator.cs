using MovieRecommendation.Application.DTOs.Recommendations;

namespace MovieRecommendation.Application.Interfaces.ML;

public interface IModelRetrainingOrchestrator
{
    Task<RetrainResultDto> RunAsync(CancellationToken cancellationToken = default);

    RetrainResultDto Enqueue();
}
