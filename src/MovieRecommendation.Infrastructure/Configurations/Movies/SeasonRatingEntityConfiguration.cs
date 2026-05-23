using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class SeasonRatingEntityConfiguration : BaseEntityConfiguration<SeasonRating>
{
    public override void Configure(EntityTypeBuilder<SeasonRating> builder)
    {
        base.Configure(builder);

        builder.ToTable("season_ratings");

        builder.Property(x => x.Score)
            .HasPrecision(5, 2);

        builder.HasIndex(x => new { x.UserId, x.SeasonId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Season)
            .WithMany(x => x.Ratings)
            .HasForeignKey(x => x.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
