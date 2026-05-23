using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Services.Email;

namespace MovieRecommendation.Infrastructure.Services;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly string _clientUrl;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IOptions<ClientSettings> clientOptions)
    {
        _logger = logger;
        _clientUrl = clientOptions.Value.Url;
    }

    public Task SendVerificationAsync(
        string toEmail,
        string username,
        Guid userId,
        string token,
        string language,
        CancellationToken ct = default)
    {
        var link = BuildVerificationLink(userId, token);
        var content = EmailTemplates.Verification(username, link, language);
        LogEmail("VERIFICATION", toEmail, language, content.Subject, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(
        string toEmail,
        string username,
        Guid userId,
        string token,
        string language,
        CancellationToken ct = default)
    {
        var link = BuildPasswordResetLink(userId, token);
        var content = EmailTemplates.PasswordReset(username, link, language);
        LogEmail("PASSWORD RESET", toEmail, language, content.Subject, link);
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(
        string toEmail,
        string username,
        string language,
        CancellationToken ct = default)
    {
        var content = EmailTemplates.Welcome(username, language);
        LogEmail("WELCOME", toEmail, language, content.Subject, link: null);
        return Task.CompletedTask;
    }

    private string BuildVerificationLink(Guid userId, string token)
        => $"{_clientUrl}/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}";

    private string BuildPasswordResetLink(Guid userId, string token)
        => $"{_clientUrl}/auth/reset-password?userId={userId}&token={Uri.EscapeDataString(token)}";

    private void LogEmail(string kind, string toEmail, string language, string subject, string? link)
    {
        _logger.LogInformation(
            "===== EMAIL [{Kind}] =====\nTo: {Email}\nLang: {Language}\nSubject: {Subject}{LinkLine}\n=================================",
            kind,
            toEmail,
            language,
            subject,
            link is null ? string.Empty : $"\nLink: {link}");
    }
}
