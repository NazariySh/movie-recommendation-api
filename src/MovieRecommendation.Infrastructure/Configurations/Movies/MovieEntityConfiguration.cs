using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class MovieEntityConfiguration : BaseEntityConfiguration<Movie>
{
    public override void Configure(EntityTypeBuilder<Movie> builder)
    {
        base.Configure(builder);

        builder.ToTable("movies");

        builder.Property(x => x.Key).HasMaxLength(255);

        builder.Property(x => x.Embedding).HasColumnType("vector(1536)");

        builder.Property(x => x.OriginalTitle).HasMaxLength(255).IsRequired();

        builder.Property(x => x.OriginalLang).HasMaxLength(10).IsRequired().HasDefaultValue("en");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(x => x.PosterUrl).HasMaxLength(500);

        builder.Property(x => x.BackdropUrl).HasMaxLength(500);

        builder.Property(x => x.TrailerYoutubeId).HasMaxLength(50);

        builder.Property(x => x.AverageRating).HasPrecision(5, 2);

        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.ReleaseDate);
        builder.HasIndex(x => x.AverageRating);
    }
}
