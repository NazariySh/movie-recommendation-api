using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Infrastructure.Configurations.Users;

internal class UserGenrePreferenceEntityConfiguration : BaseEntityConfiguration<UserGenrePreference>
{
    public override void Configure(EntityTypeBuilder<UserGenrePreference> builder)
    {
        base.Configure(builder);

        builder.ToTable("user_genre_preferences");

        builder.Property(x => x.Weight)
            .HasPrecision(5, 2);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Genre)
            .WithMany()
            .HasForeignKey(x => x.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
