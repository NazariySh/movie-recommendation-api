namespace MovieRecommendation.Domain.Settings;

public class GoogleAuthSettings
{
    public const string SectionName = "GoogleAuth";

    public string? ClientId { get; set; }
}
