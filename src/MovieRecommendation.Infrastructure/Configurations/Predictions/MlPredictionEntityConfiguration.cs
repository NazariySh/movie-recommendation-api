using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Predictions;

namespace MovieRecommendation.Infrastructure.Configurations.Predictions;

internal class MlPredictionEntityConfiguration : BaseEntityConfiguration<MlPrediction>
{
    public override void Configure(EntityTypeBuilder<MlPrediction> builder)
    {
        base.Configure(builder);

        builder.ToTable("ml_predictions");

        builder.Property(x => x.PredictedScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.ModelVersion)
            .HasMaxLength(50);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Movie)
            .WithMany()
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
