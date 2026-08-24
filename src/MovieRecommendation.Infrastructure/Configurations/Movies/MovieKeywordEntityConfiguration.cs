using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class MovieKeywordEntityConfiguration : IEntityTypeConfiguration<MovieKeyword>
{
    public void Configure(EntityTypeBuilder<MovieKeyword> builder)
    {
        builder.ToTable("movie_keywords");

        builder.HasKey(x => new { x.MovieId, x.Name });

        builder.Property(x => x.Name).HasMaxLength(100);

        builder.HasOne(x => x.Movie)
            .WithMany(x => x.Keywords)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Name);
    }
}
