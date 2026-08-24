using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Infrastructure.Services;

public class TmdbPersonImporter : ITmdbPersonImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _httpClient;
    private readonly TmdbSettings _settings;
    private readonly ILogger<TmdbPersonImporter> _logger;

    public TmdbPersonImporter(
        HttpClient httpClient,
        IOptions<TmdbSettings> options,
        ILogger<TmdbPersonImporter> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        }
    }

    public async Task<TmdbPersonResult?> FetchByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            _logger.LogDebug("TMDb skipped: AccessToken not configured");
            return null;
        }

        var findUri = $"find/{imdbId}?external_source=imdb_id";

        using var findResponse = await _httpClient.GetAsync(findUri, cancellationToken);

        if (findResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        findResponse.EnsureSuccessStatusCode();

        var findPayload = await JsonSerializer.DeserializeAsync<TmdbFindResponse>(
            await findResponse.Content.ReadAsStreamAsync(cancellationToken),
            JsonOptions,
            cancellationToken);

        var match = findPayload?.PersonResults?.FirstOrDefault();
        if (match is null)
        {
            return null;
        }

        var personUri = $"person/{match.Id}";

        using var personResponse = await _httpClient.GetAsync(personUri, cancellationToken);
        personResponse.EnsureSuccessStatusCode();

        var person = await JsonSerializer.DeserializeAsync<TmdbPersonResponse>(
            await personResponse.Content.ReadAsStreamAsync(cancellationToken),
            JsonOptions,
            cancellationToken);

        if (person is null)
        {
            return null;
        }

        return new TmdbPersonResult
        {
            TmdbId = person.Id,
            ImdbId = person.ImdbId ?? imdbId,
            Name = person.Name ?? string.Empty,
            Biography = string.IsNullOrWhiteSpace(person.Biography) ? null : person.Biography,
            Birthday = ParseDate(person.Birthday),
            DateOfDeath = ParseDate(person.Deathday),
            PlaceOfBirth = person.PlaceOfBirth,
            KnownForDepartment = person.KnownForDepartment,
            Gender = MapGender(person.Gender),
            ProfileImageUrl = string.IsNullOrWhiteSpace(person.ProfilePath)
                ? null
                : $"{_settings.ImageBaseUrl.TrimEnd('/')}{person.ProfilePath}",
        };
    }

    private static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    private static string? MapGender(int? code) => code switch
    {
        1 => "Female",
        2 => "Male",
        3 => "Non-binary",
        _ => null,
    };

    private sealed class TmdbFindResponse
    {
        [JsonPropertyName("person_results")]
        public List<TmdbFindPerson>? PersonResults { get; set; }
    }

    private sealed class TmdbFindPerson
    {
        public int Id { get; set; }
    }

    private sealed class TmdbPersonResponse
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Biography { get; set; }

        public string? Birthday { get; set; }

        public string? Deathday { get; set; }

        [JsonPropertyName("place_of_birth")]
        public string? PlaceOfBirth { get; set; }

        [JsonPropertyName("known_for_department")]
        public string? KnownForDepartment { get; set; }

        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }

        public int? Gender { get; set; }
    }
}
