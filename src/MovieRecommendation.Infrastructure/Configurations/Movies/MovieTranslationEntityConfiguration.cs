using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class MovieTranslationEntityConfiguration : IEntityTypeConfiguration<MovieTranslation>
{
    public void Configure(EntityTypeBuilder<MovieTranslation> builder)
    {
        builder.ToTable("movie_translations");

        builder.HasKey(x => new { x.MovieId, x.LanguageCode });

        builder.Property(x => x.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Overview)
            .HasMaxLength(2000);

        builder.Property(x => x.Tagline)
            .HasMaxLength(500);

        builder.HasOne(x => x.Movie)
            .WithMany(m => m.Translations)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LanguageCode);
    }
}
