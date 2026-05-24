namespace MovieRecommendation.Domain.Settings;

public class SeedingSettings
{
    public const string SectionName = "Seeding";

    public string? DataPath { get; set; }

    public string? AdminEmail { get; set; }

    public string? AdminUsername { get; set; }

    public string? AdminPassword { get; set; }

    public int MovieCount { get; set; } = 5000;

    public int MovieLensUserCount { get; set; } = 1000;

    public int MaxRatingsPerUser { get; set; } = 200;
}
