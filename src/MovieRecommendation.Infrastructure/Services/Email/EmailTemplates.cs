using System.Net;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.Infrastructure.Services.Email;

public record EmailContent(string Subject, string HtmlBody, string TextBody);

public static class EmailTemplates
{
    public const string DefaultLanguage = LanguageCodes.Default;
    private const string UkrainianLanguage = LanguageCodes.Ukrainian;

    public static EmailContent Verification(string username, string link, string language)
    {
        var lang = Normalize(language);
        var name = WebUtility.HtmlEncode(username);
        var safeLink = WebUtility.HtmlEncode(link);

        return lang == UkrainianLanguage
            ? new EmailContent(
                Subject: "Підтвердьте свою адресу — MovieMatch",
                HtmlBody: Layout(
                    title: "Підтвердження пошти",
                    heading: $"Вітаємо, {name}!",
                    body: "Залишився останній крок — підтвердіть свою адресу електронної пошти, натиснувши кнопку нижче.",
                    ctaText: "Підтвердити пошту",
                    ctaLink: safeLink,
                    footerNote: "Якщо ви не реєструвалися на MovieMatch, просто проігноруйте цей лист."),
                TextBody: $"Вітаємо, {username}!\n\nПідтвердьте пошту: {link}\n\nЯкщо це не ви — проігноруйте цей лист.")
            : new EmailContent(
                Subject: "Confirm your email — MovieMatch",
                HtmlBody: Layout(
                    title: "Confirm your email",
                    heading: $"Welcome, {name}!",
                    body: "One last step — confirm your email address by clicking the button below.",
                    ctaText: "Confirm email",
                    ctaLink: safeLink,
                    footerNote: "If you didn't sign up for MovieMatch, you can safely ignore this email."),
                TextBody: $"Welcome, {username}!\n\nConfirm your email: {link}\n\nIf this wasn't you, ignore this message.");
    }

    public static EmailContent PasswordReset(string username, string link, string language)
    {
        var lang = Normalize(language);
        var name = WebUtility.HtmlEncode(username);
        var safeLink = WebUtility.HtmlEncode(link);

        return lang == UkrainianLanguage
            ? new EmailContent(
                Subject: "Скидання паролю — MovieMatch",
                HtmlBody: Layout(
                    title: "Скидання паролю",
                    heading: $"Привіт, {name}.",
                    body: "Ми отримали запит на зміну паролю до вашого облікового запису. Якщо це були ви — натисніть на кнопку нижче, щоб встановити новий пароль. Посилання дійсне обмежений час.",
                    ctaText: "Скинути пароль",
                    ctaLink: safeLink,
                    footerNote: "Якщо ви не запитували скидання, просто проігноруйте цей лист — пароль не буде змінено."),
                TextBody: $"Привіт, {username}.\n\nЩоб скинути пароль, перейдіть за посиланням: {link}\n\nЯкщо це не ви — проігноруйте лист.")
            : new EmailContent(
                Subject: "Reset your password — MovieMatch",
                HtmlBody: Layout(
                    title: "Reset your password",
                    heading: $"Hi {name},",
                    body: "We received a request to reset the password on your account. If it was you, click the button below to set a new password. This link is valid for a limited time.",
                    ctaText: "Reset password",
                    ctaLink: safeLink,
                    footerNote: "If you didn't request a reset, just ignore this email — your password won't change."),
                TextBody: $"Hi {username},\n\nReset your password: {link}\n\nIf this wasn't you, ignore this message.");
    }

    public static EmailContent Welcome(string username, string language)
    {
        var lang = Normalize(language);
        var name = WebUtility.HtmlEncode(username);

        return lang == UkrainianLanguage
            ? new EmailContent(
                Subject: "Ласкаво просимо до MovieMatch",
                HtmlBody: Layout(
                    title: "Ласкаво просимо",
                    heading: $"Привіт, {name}!",
                    body: "Раді бачити вас у MovieMatch. Тепер ви можете оцінювати фільми, складати списки до перегляду й отримувати персональні рекомендації.",
                    ctaText: null,
                    ctaLink: null,
                    footerNote: "Гарного перегляду!"),
                TextBody: $"Привіт, {username}!\n\nЛаскаво просимо до MovieMatch. Гарного перегляду!")
            : new EmailContent(
                Subject: "Welcome to MovieMatch",
                HtmlBody: Layout(
                    title: "Welcome",
                    heading: $"Hi {name}!",
                    body: "Glad to have you on MovieMatch. You can now rate movies, build watchlists, and get personalised recommendations.",
                    ctaText: null,
                    ctaLink: null,
                    footerNote: "Enjoy the show."),
                TextBody: $"Hi {username},\n\nWelcome to MovieMatch. Enjoy the show.");
    }

    private static string Normalize(string? language)
        => string.IsNullOrWhiteSpace(language)
            ? DefaultLanguage
            : language.Trim().ToLowerInvariant();

    private static string Layout(
        string title,
        string heading,
        string body,
        string? ctaText,
        string? ctaLink,
        string footerNote)
    {
        var ctaBlock = ctaText is null || ctaLink is null
            ? string.Empty
            : $"""
              <p style="text-align:center;margin:32px 0;">
                <a href="{ctaLink}" style="background:#2283DC;color:#ffffff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;font-weight:600;">{ctaText}</a>
              </p>
              <p style="font-size:13px;color:#666;word-break:break-all;">{ctaLink}</p>
              """;

        return $"""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"><title>{title}</title></head>
            <body style="margin:0;padding:0;background:#f5f5f7;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Arial,sans-serif;color:#1d1e1f;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                <tr><td align="center" style="padding:32px 16px;">
                  <table role="presentation" width="560" cellspacing="0" cellpadding="0" border="0" style="background:#ffffff;border-radius:12px;padding:32px;max-width:100%;">
                    <tr><td>
                      <h1 style="margin:0 0 16px;font-size:22px;">{heading}</h1>
                      <p style="margin:0;line-height:1.6;font-size:15px;">{body}</p>
                      {ctaBlock}
                      <hr style="border:none;border-top:1px solid #eee;margin:32px 0 16px;">
                      <p style="font-size:13px;color:#888;margin:0;">{footerNote}</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
    }
}
