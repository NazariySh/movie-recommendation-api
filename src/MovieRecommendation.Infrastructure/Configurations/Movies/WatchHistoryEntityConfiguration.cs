using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class WatchHistoryEntityConfiguration : BaseEntityConfiguration<WatchHistory>
{
    public override void Configure(EntityTypeBuilder<WatchHistory> builder)
    {
        base.Configure(builder);

        builder.ToTable("watch_history");

        builder.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();
        builder.HasIndex(x => x.WatchedAt);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Movie)
            .WithMany(m => m.WatchHistory)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
