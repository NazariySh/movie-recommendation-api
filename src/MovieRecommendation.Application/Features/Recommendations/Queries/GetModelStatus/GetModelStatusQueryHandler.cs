using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Recommendations;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetModelStatus;

public class GetModelStatusQueryHandler : IQueryHandler<GetModelStatusQuery, ModelStatusDto>
{
    private readonly IMlModelMetadataRepository _repository;

    public GetModelStatusQueryHandler(IMlModelMetadataRepository repository)
    {
        _repository = repository;
    }

    public async Task<ModelStatusDto> Handle(GetModelStatusQuery request, CancellationToken cancellationToken)
    {
        var meta = await _repository.GetActiveAsync(cancellationToken);

        if (meta is null)
        {
            return new ModelStatusDto { IsActive = false };
        }

        return new ModelStatusDto
        {
            Version = meta.Version,
            TrainedAt = meta.TrainedAt,
            Rmse = meta.Rmse,
            R2 = meta.R2,
            SampleCount = meta.SampleCount,
            IsActive = meta.IsActive,
        };
    }
}
