using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.API.Extensions;

public static class HttpContextExtensions
{
    private const string DefaultLang = LanguageCodes.Default;

    public static string GetRequestLanguage(this HttpContext ctx)
    {
        var header = ctx.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return DefaultLang;
        }

        var first = header.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (first is null)
        {
            return DefaultLang;
        }

        var code = first.Split(';')[0].Trim();
        var primary = code.Split('-')[0].ToLowerInvariant();

        return LanguageCodes.SupportedLanguages.Contains(primary) ? primary : DefaultLang;
    }
}
