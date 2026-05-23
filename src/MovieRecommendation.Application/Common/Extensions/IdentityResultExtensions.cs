using Microsoft.AspNetCore.Identity;

namespace MovieRecommendation.Application.Common.Extensions;

public static class IdentityResultExtensions
{
    public static string FormatErrors(this IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));

    public static void EnsureSucceeded(this IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException($"{message}: {result.FormatErrors()}");
    }
}
