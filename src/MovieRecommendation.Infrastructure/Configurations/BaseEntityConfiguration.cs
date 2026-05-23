using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities;

namespace MovieRecommendation.Infrastructure.Configurations;

internal abstract class BaseEntityConfiguration<TEntity> : BaseEntityConfiguration<TEntity, Guid>
    where TEntity : BaseEntity<Guid>
{
}

internal abstract class BaseEntityConfiguration<TEntity, TKey> : AuditableEntityConfiguration<TEntity>
    where TEntity : BaseEntity<TKey>
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
    }
}
