using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Predictions;

namespace MovieRecommendation.Infrastructure.Configurations.Predictions;

internal class MlModelMetadataEntityConfiguration : BaseEntityConfiguration<MlModelMetadata>
{
    public override void Configure(EntityTypeBuilder<MlModelMetadata> builder)
    {
        base.Configure(builder);

        builder.ToTable("ml_model_metadata");

        builder.Property(x => x.Version)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.TrainedAt);
        builder.HasIndex(x => x.IsActive);
    }
}
