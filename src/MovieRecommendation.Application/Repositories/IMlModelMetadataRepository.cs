using MovieRecommendation.Domain.Entities.Predictions;

namespace MovieRecommendation.Application.Repositories;

public interface IMlModelMetadataRepository
{
    Task<MlModelMetadata?> GetLatestAsync(CancellationToken cancellationToken = default);

    Task<MlModelMetadata?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task RecordAsync(MlModelMetadata metadata, bool promoteAsActive, CancellationToken cancellationToken = default);
}
