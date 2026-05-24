using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Common.Extensions;
using MovieRecommendation.Domain.Constants;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Infrastructure.Data.Seeding.Seeders;

public sealed class IdentitySeeder : IIdentitySeeder
{
    private readonly UserManager<User> _userManager;
    private readonly SeedingSettings _settings;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        UserManager<User> userManager,
        IOptions<SeedingSettings> settings,
        ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager;
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SeedAsync(CancellationToken cancellationToken = default) =>
        EnsureAdminAsync(cancellationToken);

    private async Task EnsureAdminAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = _settings.AdminEmail;
        var username = _settings.AdminUsername;
        var password = _settings.AdminPassword;

        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("Admin credentials not configured; skipping admin seed");
            return;
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            await EnsureAdminRoleAsync(existing);
            return;
        }

        var admin = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            PreferredLanguage = LanguageCodes.Ukrainian,
            CreatedAt = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(admin, password);
        createResult.EnsureSucceeded("create admin user");

        await EnsureAdminRoleAsync(admin);

        _logger.LogWarning(
            "Default admin '{Email}' created with the seeded password. Change it immediately in production.",
            email);
    }

    private async Task EnsureAdminRoleAsync(User user)
    {
        var roleName = nameof(RoleType.Admin);
        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            return;
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        result.EnsureSucceeded("assign admin role");
    }
}
