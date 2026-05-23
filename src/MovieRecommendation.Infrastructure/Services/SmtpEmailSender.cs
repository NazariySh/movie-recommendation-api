using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.Infrastructure.Services.Email;

namespace MovieRecommendation.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly string _clientUrl;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> emailOptions,
        IOptions<ClientSettings> clientOptions,
        ILogger<SmtpEmailSender> logger)
    {
        _emailSettings = emailOptions.Value;
        _clientUrl = clientOptions.Value.Url;
        _logger = logger;
    }

    public Task SendVerificationAsync(
        string toEmail,
        string username,
        Guid userId,
        string token,
        string language,
        CancellationToken ct = default)
    {
        var link = $"{_clientUrl}/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}";
        var content = EmailTemplates.Verification(username, link, language);
        return SendAsync(toEmail, content, ct);
    }

    public Task SendPasswordResetAsync(
        string toEmail,
        string username,
        Guid userId,
        string token,
        string language,
        CancellationToken ct = default)
    {
        var link = $"{_clientUrl}/auth/reset-password?userId={userId}&token={Uri.EscapeDataString(token)}";
        var content = EmailTemplates.PasswordReset(username, link, language);
        return SendAsync(toEmail, content, ct);
    }

    public Task SendWelcomeAsync(
        string toEmail,
        string username,
        string language,
        CancellationToken ct = default)
    {
        var content = EmailTemplates.Welcome(username, language);
        return SendAsync(toEmail, content, ct);
    }

    private async Task SendAsync(string toEmail, EmailContent content, CancellationToken ct)
    {
        var smtp = _emailSettings.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException("Email:Smtp:Host is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = content.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = content.HtmlBody,
            TextBody = content.TextBody,
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = smtp.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        try
        {
            await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, ct);

            if (smtp.RequireAuth && !string.IsNullOrEmpty(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, ct);
            }

            await client.SendAsync(message, ct);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, ct);
            }
        }

        _logger.LogInformation("Email sent to {Recipient}: {Subject}", toEmail, content.Subject);
    }
}
