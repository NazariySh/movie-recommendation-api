using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Reviews;

namespace MovieRecommendation.Infrastructure.Configurations.Reviews;

internal class MovieReviewEntityConfiguration : BaseEntityConfiguration<MovieReview>
{
    public override void Configure(EntityTypeBuilder<MovieReview> builder)
    {
        base.Configure(builder);

        builder.ToTable("movie_reviews");

        builder.Property(x => x.Body)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(x => x.Score)
            .HasPrecision(5, 2);

        builder.HasIndex(x => new { x.UserId, x.MovieId });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Movie)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ParentReview)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentReviewId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.HelpfulVotes)
            .WithOne(v => v.Review)
            .HasForeignKey(v => v.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
