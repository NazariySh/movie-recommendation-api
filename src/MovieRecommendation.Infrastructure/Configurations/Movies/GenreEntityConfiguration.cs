using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class GenreEntityConfiguration : BaseEntityConfiguration<Genre, int>
{
    public override void Configure(EntityTypeBuilder<Genre> builder)
    {
        base.Configure(builder);

        builder.ToTable("genres");

        builder.Property(x => x.Slug)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
