using MovieRecommendation.Application.Models;

namespace MovieRecommendation.Application.Interfaces;

public interface IGoogleAuthValidator
{
    Task<GooglePayload> ValidateAsync(string idToken, CancellationToken ct = default);
}
