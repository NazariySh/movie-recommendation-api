using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class MovieRatingEntityConfiguration : BaseEntityConfiguration<MovieRating>
{
    public override void Configure(EntityTypeBuilder<MovieRating> builder)
    {
        base.Configure(builder);

        builder.ToTable("movie_ratings");

        builder.Property(x => x.Score)
            .HasPrecision(5, 2);

        builder.HasIndex(x => new { x.UserId, x.MovieId }).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Movie)
            .WithMany(x => x.Ratings)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
