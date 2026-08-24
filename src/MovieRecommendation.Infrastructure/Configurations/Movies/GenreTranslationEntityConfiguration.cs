using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class GenreTranslationEntityConfiguration : IEntityTypeConfiguration<GenreTranslation>
{
    public void Configure(EntityTypeBuilder<GenreTranslation> builder)
    {
        builder.ToTable("genre_translations");

        builder.HasKey(x => new { x.GenreId, x.LanguageCode });

        builder.Property(x => x.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(x => x.Genre)
            .WithMany(g => g.Translations)
            .HasForeignKey(x => x.GenreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LanguageCode);
    }
}
