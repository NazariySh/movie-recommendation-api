using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class WatchlistItemEntityConfiguration : BaseEntityConfiguration<WatchlistItem>
{
    public override void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("watchlist_items");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.Status });

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
