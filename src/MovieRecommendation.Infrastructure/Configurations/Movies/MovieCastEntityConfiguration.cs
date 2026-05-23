using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class MovieCastEntityConfiguration : IEntityTypeConfiguration<MovieCast>
{
    public void Configure(EntityTypeBuilder<MovieCast> builder)
    {
        builder.ToTable("movie_cast");

        builder.HasKey(x => new { x.MovieId, x.PersonId });

        builder.Property(x => x.Role)
            .HasMaxLength(100);

        builder.Property(x => x.Character)
            .HasMaxLength(255);

        builder.HasOne(x => x.Movie)
            .WithMany(x => x.MovieCasts)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Person)
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
