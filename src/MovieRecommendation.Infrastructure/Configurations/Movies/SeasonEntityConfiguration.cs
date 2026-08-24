using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class SeasonEntityConfiguration : BaseEntityConfiguration<Season>
{
    public override void Configure(EntityTypeBuilder<Season> builder)
    {
        base.Configure(builder);

        builder.ToTable("seasons");

        builder.Property(x => x.Name).HasMaxLength(255);
        builder.Property(x => x.Overview).HasMaxLength(4000);
        builder.Property(x => x.PosterUrl).HasMaxLength(500);
        builder.Property(x => x.VoteAverage).HasPrecision(5, 2);

        builder.HasOne(x => x.Movie)
            .WithMany(x => x.Seasons)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.MovieId, x.SeasonNumber }).IsUnique();
    }
}
