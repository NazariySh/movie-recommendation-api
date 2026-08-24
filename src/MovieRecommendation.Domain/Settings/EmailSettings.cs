using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.Domain.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Logging";

    public string FromAddress { get; set; } = "noreply@moviematch.app";

    public string FromName { get; set; } = "MovieMatch";

    public string DefaultLanguage { get; set; } = LanguageCodes.Default;

    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool UseStartTls { get; set; } = true;

    public bool RequireAuth { get; set; } = true;
}
