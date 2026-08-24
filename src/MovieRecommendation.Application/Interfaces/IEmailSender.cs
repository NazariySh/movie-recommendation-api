namespace MovieRecommendation.Application.Interfaces;

public interface IEmailSender
{
    Task SendVerificationAsync(
        string toEmail,
        string username,
        Guid userId,
        string token,
        string language,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail,
        string username,
        Guid userId,
        string token,
        string language,
        CancellationToken ct = default);

    Task SendWelcomeAsync(
        string toEmail,
        string username,
        string language,
        CancellationToken ct = default);
}
