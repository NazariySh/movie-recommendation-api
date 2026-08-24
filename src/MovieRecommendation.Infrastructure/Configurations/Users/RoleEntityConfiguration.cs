using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Infrastructure.Configurations.Users;

internal class RoleEntityConfiguration : AuditableEntityConfiguration<Role>
{
    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        base.Configure(builder);

        builder.ToTable("roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.NormalizedName)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasData(GetRoles());
    }

    private static Role[] GetRoles()
    {
        return
        [
            new Role
            {
                Id = Guid.Parse("a1d3c7e0-0000-0000-0000-000000000001"),
                Name = RoleType.Admin.ToString(),
                NormalizedName = RoleType.Admin.ToString().ToUpper(),
                ConcurrencyStamp = "a1d3c7e0-0000-0000-0000-000000000001",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new Role
            {
                Id = Guid.Parse("a1d3c7e0-0000-0000-0000-000000000002"),
                Name = RoleType.Moderator.ToString(),
                NormalizedName = RoleType.Moderator.ToString().ToUpper(),
                ConcurrencyStamp = "a1d3c7e0-0000-0000-0000-000000000002",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new Role
            {
                Id = Guid.Parse("a1d3c7e0-0000-0000-0000-000000000003"),
                Name = RoleType.User.ToString(),
                NormalizedName = RoleType.User.ToString().ToUpper(),
                ConcurrencyStamp = "a1d3c7e0-0000-0000-0000-000000000003",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
        ];
    }
}
