using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Predictions;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class MlModelMetadataRepository : BaseRepository<MlModelMetadata>, IMlModelMetadataRepository
{
    public MlModelMetadataRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public Task<MlModelMetadata?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.MlModelMetadata
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.TrainedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MlModelMetadata?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var active = await DbContext.MlModelMetadata
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.TrainedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return active ?? await GetLatestAsync(cancellationToken);
    }

    public async Task RecordAsync(MlModelMetadata metadata, bool promoteAsActive, CancellationToken cancellationToken = default)
    {
        if (promoteAsActive)
        {
            await DbContext.MlModelMetadata
                .Where(x => x.IsActive)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.IsActive, false)
                          .SetProperty(x => x.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            metadata.IsActive = true;
        }

        DbContext.MlModelMetadata.Add(metadata);
    }
}
