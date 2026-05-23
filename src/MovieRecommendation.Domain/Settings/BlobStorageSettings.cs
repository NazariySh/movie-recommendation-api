namespace MovieRecommendation.Domain.Settings;

public class BlobStorageSettings
{
    public const string SectionName = "BlobStorageSettings";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "moviematch";

    public string? PublicBaseUrl { get; set; }
}
