using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Infrastructure.Configurations.Users;

internal class SurveyResponseEntityConfiguration : BaseEntityConfiguration<SurveyResponse>
{
    public override void Configure(EntityTypeBuilder<SurveyResponse> builder)
    {
        base.Configure(builder);

        builder.ToTable("survey_responses");

        builder.Property(x => x.Answers)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
