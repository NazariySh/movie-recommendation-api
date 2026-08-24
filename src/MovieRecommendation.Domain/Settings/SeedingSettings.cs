namespace MovieRecommendation.Domain.Settings;

public class SeedingSettings
{
    public const string SectionName = "Seeding";

    public string? DataPath { get; set; }

    public string? AdminEmail { get; set; }

    public string? AdminUsername { get; set; }

    public string? AdminPassword { get; set; }

    public int MovieCount { get; set; } = 9742;

    public int MovieLensUserCount { get; set; } = 610;

    public int MaxRatingsPerUser { get; set; } = 5000;
}
