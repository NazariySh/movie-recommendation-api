namespace MovieRecommendation.Domain.Settings;

public class TmdbSettings
{
    public string AccessToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; }

    public string ImageBaseUrl { get; set; }
}
