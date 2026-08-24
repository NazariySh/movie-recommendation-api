using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Models;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Infrastructure.Services;

public class GoogleAuthValidator : IGoogleAuthValidator
{
    private const string GoogleTokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo?id_token=";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleAuthValidator> _logger;
    private readonly string? _clientId;

    public GoogleAuthValidator(
        HttpClient httpClient,
        IOptions<GoogleAuthSettings> googleAuthOptions,
        ILogger<GoogleAuthValidator> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _clientId = googleAuthOptions.Value.ClientId;
    }

    public async Task<GooglePayload> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException("Google idToken is required.");
        }

        var response = await _httpClient.GetAsync(GoogleTokenInfoUrl + Uri.EscapeDataString(idToken), ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google token validation failed: {Status}", response.StatusCode);
            throw new UnauthorizedAccessException("Invalid Google token.");
        }

        var info = await response.Content.ReadFromJsonAsync<GoogleTokenInfo>(cancellationToken: ct)
            ?? throw new UnauthorizedAccessException("Invalid Google token payload.");

        if (!string.IsNullOrEmpty(_clientId) && info.Aud != _clientId)
        {
            throw new UnauthorizedAccessException("Google token aud mismatch.");
        }

        return new GooglePayload
        {
            Email = info.Email ?? throw new UnauthorizedAccessException("Google token has no email."),
            EmailVerified = info.EmailVerified == "true",
            GivenName = info.GivenName,
            FamilyName = info.FamilyName,
            Picture = info.Picture,
            Subject = info.Sub ?? string.Empty,
        };
    }

    private sealed class GoogleTokenInfo
    {
        [JsonPropertyName("aud")] public string? Aud { get; set; }
        [JsonPropertyName("sub")] public string? Sub { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("email_verified")] public string? EmailVerified { get; set; }
        [JsonPropertyName("given_name")] public string? GivenName { get; set; }
        [JsonPropertyName("family_name")] public string? FamilyName { get; set; }
        [JsonPropertyName("picture")] public string? Picture { get; set; }
    }
}
