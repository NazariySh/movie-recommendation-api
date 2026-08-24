using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Data.Interceptors;
using MovieRecommendation.Infrastructure.Data.Seeding;
using MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;
using MovieRecommendation.Infrastructure.Data.Seeding.Seeders;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.Infrastructure.Services;

namespace MovieRecommendation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<BlobStorageSettings>(configuration.GetSection(BlobStorageSettings.SectionName));
        services.Configure<TmdbSettings>(configuration.GetSection(nameof(TmdbSettings)));
        services.Configure<SurveySettings>(configuration.GetSection(SurveySettings.SectionName));
        services.Configure<RecommendationSettings>(configuration.GetSection(RecommendationSettings.SectionName));
        services.Configure<SeedingSettings>(configuration.GetSection(SeedingSettings.SectionName));
        services.Configure<GoogleAuthSettings>(configuration.GetSection(GoogleAuthSettings.SectionName));
        services.Configure<ClientSettings>(configuration.GetSection(ClientSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<OpenAiSettings>(configuration.GetSection(OpenAiSettings.SectionName));

        services.AddMemoryCache();

        services.AddIdentity<User, Role>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<AuditableEntitiesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.UseVector())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditableEntitiesInterceptor>())
        );

        services.AddScoped<ITokenProvider, JwtTokenProvider>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IMovieReviewRepository, MovieReviewRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<IWatchHistoryRepository, WatchHistoryRepository>();
        services.AddScoped<IMlModelMetadataRepository, MlModelMetadataRepository>();
        services.AddScoped<IUserGenrePreferenceRepository, UserGenrePreferenceRepository>();
        services.AddScoped<ISurveyResponseRepository, SurveyResponseRepository>();
        services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();

        services.AddHttpContextAccessor();

        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<IClientInfoProvider, HttpClientInfoProvider>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        var emailProvider = configuration.GetSection("Email:Provider").Value;
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }
        services.AddHttpClient<IGoogleAuthValidator, GoogleAuthValidator>(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddHttpClient<ITmdbPersonImporter, TmdbPersonImporter>();
        services.AddHttpClient<IExternalMovieDataProvider, TmdbMovieProvider>();
        services.AddHttpClient(nameof(AzureBlobStorageService));

        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        services.AddSingleton<IImageMirrorService, ImageMirrorService>();

        services.AddSingleton<IMovieLensCsvReader, MovieLensCsvReader>();
        services.AddScoped<IIdentitySeeder, IdentitySeeder>();
        services.AddScoped<IGenreSeeder, GenreSeeder>();
        services.AddScoped<IMovieSeeder, MovieSeeder>();
        services.AddScoped<IMovieLensUserSeeder, MovieLensUserSeeder>();
        services.AddScoped<IMovieLensRatingSeeder, MovieLensRatingSeeder>();
        services.AddScoped<IMovieLensRatingBackfillSeeder, MovieLensRatingBackfillSeeder>();
        services.AddScoped<ITranslationSeeder, TranslationSeeder>();
        services.AddScoped<DataSeeder>();

        return services;
    }
}
