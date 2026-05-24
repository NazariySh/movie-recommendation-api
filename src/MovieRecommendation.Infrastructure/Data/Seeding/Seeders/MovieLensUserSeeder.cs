using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Infrastructure.Data.Seeding.Constants;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class MovieLensUserSeeder : IMovieLensUserSeeder
{
    private const string UsernamePrefix = "mlens_user_";

    private readonly UserManager<User> _userManager;
    private readonly ILogger<MovieLensUserSeeder> _logger;

    public MovieLensUserSeeder(
        UserManager<User> userManager,
        ILogger<MovieLensUserSeeder> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public static string UsernameForMovieLensId(int movieLensId) => $"{UsernamePrefix}{movieLensId:D3}";

    public static string EmailForMovieLensId(int movieLensId) =>
        $"{UsernameForMovieLensId(movieLensId)}@{SeedingConstants.MovieLensUserEmailDomain}";

    public async Task<IReadOnlyDictionary<int, Guid>> SeedAsync(int count, CancellationToken cancellationToken = default)
    {
        var mapping = new Dictionary<int, Guid>(count);
        var created = 0;
        var now = DateTime.UtcNow;

        for (var movieLensId = 1; movieLensId <= count; movieLensId++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var email = EmailForMovieLensId(movieLensId);
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                mapping[movieLensId] = existing.Id;
                continue;
            }

            var user = new User
            {
                UserName = UsernameForMovieLensId(movieLensId),
                Email = email,
                EmailConfirmed = true,
                OnboardingCompleted = true,
                PreferredLanguage = LanguageCodes.English,
                CreatedAt = now,
            };

            var result = await _userManager.CreateAsync(user, SeedingConstants.MovieLensUserPassword);
            result.EnsureSucceeded($"create MovieLens user {user.UserName}");

            mapping[movieLensId] = user.Id;
            created++;
        }

        if (created > 0)
        {
            _logger.LogInformation(
                "Created {Created} MovieLens users (total mapped: {Total})",
                created,
                mapping.Count);
        }
        else
        {
            _logger.LogInformation("MovieLens users already present ({Count} mapped)", mapping.Count);
        }

        return mapping;
    }
}
