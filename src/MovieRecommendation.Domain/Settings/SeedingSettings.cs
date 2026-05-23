namespace MovieRecommendation.Domain.Settings;

public class SeedingSettings
{
    public const string SectionName = "Seeding";

    public string? DataPath { get; set; }

    public string? AdminEmail { get; set; }

    public string? AdminUsername { get; set; }

    public string? AdminPassword { get; set; }
}
