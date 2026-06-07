using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Domain.Entities;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Predictions;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Infrastructure.Configurations.Movies;

namespace MovieRecommendation.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<UserGenrePreference> UserGenrePreferences { get; set; }

    public DbSet<SurveyResponse> SurveyResponses { get; set; }

    public DbSet<Movie> Movies { get; set; }

    public DbSet<MovieTranslation> MovieTranslations { get; set; }

    public DbSet<Genre> Genres { get; set; }

    public DbSet<GenreTranslation> GenreTranslations { get; set; }

    public DbSet<MovieKeyword> MovieKeywords { get; set; }

    public DbSet<Season> Seasons { get; set; }

    public DbSet<Person> People { get; set; }

    public DbSet<MovieCast> MovieCasts { get; set; }

    public DbSet<MovieRating> Ratings { get; set; }

    public DbSet<WatchlistItem> WatchlistItems { get; set; }

    public DbSet<WatchHistory> WatchHistory { get; set; }

    public DbSet<MovieReview> MovieReviews { get; set; }

    public DbSet<ReviewHelpfulVote> ReviewHelpfulVotes { get; set; }

    public DbSet<MlPrediction> MlPredictions { get; set; }

    public DbSet<MlModelMetadata> MlModelMetadata { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("vector");
        builder.HasPostgresExtension("pg_trgm");
        builder.ApplyConfigurationsFromAssembly(typeof(MovieEntityConfiguration).Assembly);

        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");

        ApplySoftDeleteFilters(builder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            typeof(ApplicationDbContext)
                .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(null, [builder]);
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ISoftDeletable
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}
