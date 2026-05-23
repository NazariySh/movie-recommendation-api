using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Infrastructure.Configurations.Movies;

internal class PersonEntityConfiguration : BaseEntityConfiguration<Person>
{
    public override void Configure(EntityTypeBuilder<Person> builder)
    {
        base.Configure(builder);

        builder.ToTable("people");

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ImdbId)
            .HasMaxLength(20);

        builder.Property(x => x.PlaceOfBirth)
            .HasMaxLength(255);

        builder.Property(x => x.Nationality)
            .HasMaxLength(100);

        builder.Property(x => x.Gender)
            .HasMaxLength(20);

        builder.Property(x => x.KnownForDepartment)
            .HasMaxLength(50);

        builder.Property(x => x.Biography)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasIndex(x => x.TmdbId);
        builder.HasIndex(x => x.ImdbId);

        builder.HasIndex(x => x.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_people_name_trgm");
    }
}
