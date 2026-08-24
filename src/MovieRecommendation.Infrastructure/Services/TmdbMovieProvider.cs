using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Infrastructure.Services;

public class TmdbMovieProvider : IExternalMovieDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _httpClient;
    private readonly TmdbSettings _settings;
    private readonly ILogger<TmdbMovieProvider> _logger;

    public TmdbMovieProvider(
        HttpClient httpClient,
        IOptions<TmdbSettings> options,
        ILogger<TmdbMovieProvider> logger)
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

    public async Task<ExternalMovieResult?> FetchByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            _logger.LogDebug("TMDb skipped: AccessToken not configured");
            return null;
        }

        var find = await GetAsync<FindResponse>(
            $"find/{imdbId}?external_source=imdb_id",
            cancellationToken);

        if (find is null)
        {
            return null;
        }

        var movieMatch = find.MovieResults?.FirstOrDefault();
        if (movieMatch is not null)
        {
            return await BuildMovieAsync(movieMatch.Id, imdbId, cancellationToken);
        }

        var tvMatch = find.TvResults?.FirstOrDefault();
        if (tvMatch is not null)
        {
            return await BuildSeriesAsync(tvMatch.Id, imdbId, cancellationToken);
        }

        return null;
    }

    public async Task<ExternalTranslationResult?> FetchTranslationAsync(
        int tmdbId,
        TitleType type,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AccessToken))
        {
            _logger.LogDebug("TMDb skipped: AccessToken not configured");
            return null;
        }

        var path = type == TitleType.Series
            ? $"tv/{tmdbId}/translations"
            : $"movie/{tmdbId}/translations";

        var response = await GetAsync<TmdbTranslations>(path, cancellationToken);

        var match = response?.Translations?.FirstOrDefault(t =>
            string.Equals(t.Iso6391, languageCode, StringComparison.OrdinalIgnoreCase));

        if (match?.Data is null)
        {
            return null;
        }

        var title = string.IsNullOrWhiteSpace(match.Data.Title) ? match.Data.Name : match.Data.Title;

        return new ExternalTranslationResult
        {
            LanguageCode = languageCode,
            Title = string.IsNullOrWhiteSpace(title) ? null : title!.Trim(),
            Overview = string.IsNullOrWhiteSpace(match.Data.Overview) ? null : match.Data.Overview,
            Tagline = string.IsNullOrWhiteSpace(match.Data.Tagline) ? null : match.Data.Tagline,
        };
    }

    private async Task<ExternalMovieResult?> BuildMovieAsync(int tmdbId, string imdbId, CancellationToken ct)
    {
        var detail = await GetAsync<TmdbMovieDetail>($"movie/{tmdbId}?append_to_response=credits,videos", ct);
        if (detail is null)
        {
            return null;
        }

        return new ExternalMovieResult
        {
            ImdbId = imdbId,
            TmdbId = tmdbId,
            Type = TitleType.Movie,
            Title = detail.Title ?? string.Empty,
            OriginalTitle = detail.OriginalTitle ?? detail.Title ?? string.Empty,
            OriginalLang = detail.OriginalLanguage ?? "en",
            Overview = detail.Overview,
            Tagline = detail.Tagline,
            ReleaseDate = ParseDate(detail.ReleaseDate),
            Runtime = detail.Runtime,
            Status = MapMovieStatus(detail.Status),
            PosterUrl = BuildImage(detail.PosterPath),
            BackdropUrl = BuildImage(detail.BackdropPath, "original"),
            TrailerYoutubeId = ExtractTrailer(detail.Videos),
            Genres = detail.Genres?.Select(g => g.Name ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? [],
            Cast = BuildCast(detail.Credits),
        };
    }

    private async Task<ExternalMovieResult?> BuildSeriesAsync(int tmdbId, string imdbId, CancellationToken ct)
    {
        var detail = await GetAsync<TmdbSeriesDetail>($"tv/{tmdbId}?append_to_response=aggregate_credits,videos", ct);
        if (detail is null)
        {
            return null;
        }

        return new ExternalMovieResult
        {
            ImdbId = imdbId,
            TmdbId = tmdbId,
            Type = TitleType.Series,
            Title = detail.Name ?? string.Empty,
            OriginalTitle = detail.OriginalName ?? detail.Name ?? string.Empty,
            OriginalLang = detail.OriginalLanguage ?? "en",
            Overview = detail.Overview,
            Tagline = detail.Tagline,
            ReleaseDate = ParseDate(detail.FirstAirDate),
            Runtime = detail.EpisodeRunTime?.FirstOrDefault(),
            SeasonsCount = detail.NumberOfSeasons,
            EpisodesCount = detail.NumberOfEpisodes,
            IsOngoing = string.Equals(detail.Status, "Returning Series", StringComparison.OrdinalIgnoreCase),
            Status = MapSeriesStatus(detail.Status),
            PosterUrl = BuildImage(detail.PosterPath),
            BackdropUrl = BuildImage(detail.BackdropPath, "original"),
            TrailerYoutubeId = ExtractTrailer(detail.Videos),
            Genres = detail.Genres?.Select(g => g.Name ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? [],
            Cast = BuildCast(detail.AggregateCredits),
            Seasons = BuildSeasons(detail.Seasons),
        };
    }

    private IReadOnlyList<ExternalMovieSeason> BuildSeasons(List<TmdbSeason>? seasons)
    {
        if (seasons is null)
        {
            return [];
        }

        return seasons
            .Where(s => s.SeasonNumber >= 0)
            .Select(s => new ExternalMovieSeason
            {
                SeasonNumber = s.SeasonNumber,
                Name = s.Name,
                Overview = s.Overview,
                EpisodeCount = s.EpisodeCount,
                AirDate = ParseDate(s.AirDate),
                VoteAverage = s.VoteAverage,
                PosterUrl = BuildImage(s.PosterPath),
            })
            .OrderBy(s => s.SeasonNumber)
            .ToList();
    }

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await JsonSerializer.DeserializeAsync<T>(
                await response.Content.ReadAsStreamAsync(ct),
                JsonOptions,
                ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "TMDb GET {Url} failed", relativeUrl);
            return null;
        }
    }

    private string? BuildImage(string? path, string size = "w500")
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var basePart = _settings.ImageBaseUrl.TrimEnd('/');
        var slash = basePart.LastIndexOf('/');
        if (slash > 0)
        {
            basePart = basePart.Substring(0, slash);
        }

        return $"{basePart}/{size}{path}";
    }

    private static string? ExtractTrailer(TmdbVideos? videos)
    {
        if (videos?.Results is null)
        {
            return null;
        }

        var match = videos.Results.FirstOrDefault(v =>
            string.Equals(v.Site, "YouTube", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(v.Type, "Trailer", StringComparison.OrdinalIgnoreCase) &&
            v.Official);

        match ??= videos.Results.FirstOrDefault(v =>
            string.Equals(v.Site, "YouTube", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(v.Type, "Trailer", StringComparison.OrdinalIgnoreCase));

        return match?.Key;
    }

    private static IReadOnlyList<ExternalMovieCast> BuildCast(TmdbCredits? credits)
    {
        if (credits is null)
        {
            return [];
        }

        var actors = (credits.Cast ?? [])
            .OrderBy(c => c.Order ?? int.MaxValue)
            .Take(10)
            .Select(c => new ExternalMovieCast
            {
                Name = c.Name ?? string.Empty,
                TmdbId = c.Id,
                Role = "Acting",
                Character = c.Character,
                CastOrder = c.Order,
                PhotoUrl = c.ProfilePath is null ? null : $"https://image.tmdb.org/t/p/w185{c.ProfilePath}",
            });

        var crew = (credits.Crew ?? [])
            .Where(c => c.Job is "Director" or "Writer" or "Screenplay" or "Producer")
            .Select(c => new ExternalMovieCast
            {
                Name = c.Name ?? string.Empty,
                TmdbId = c.Id,
                Role = c.Department ?? c.Job ?? "Crew",
                PhotoUrl = c.ProfilePath is null ? null : $"https://image.tmdb.org/t/p/w185{c.ProfilePath}",
            });

        return actors.Concat(crew).Where(c => !string.IsNullOrWhiteSpace(c.Name)).ToList();
    }

    private static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
            ? dt
            : null;
    }

    private static MovieStatus MapMovieStatus(string? raw) => raw?.ToLowerInvariant() switch
    {
        "released" => MovieStatus.Released,
        "post production" => MovieStatus.PostProduction,
        "in production" => MovieStatus.InProduction,
        "planned" => MovieStatus.Planned,
        "canceled" => MovieStatus.Canceled,
        "rumored" => MovieStatus.Announced,
        _ => MovieStatus.Unknown,
    };

    private static MovieStatus MapSeriesStatus(string? raw) => raw?.ToLowerInvariant() switch
    {
        "returning series" => MovieStatus.Returning,
        "ended" => MovieStatus.Ended,
        "canceled" => MovieStatus.Canceled,
        "in production" => MovieStatus.InProduction,
        "planned" => MovieStatus.Planned,
        _ => MovieStatus.Unknown,
    };


    private sealed class FindResponse
    {
        [JsonPropertyName("movie_results")]
        public List<FindHit>? MovieResults { get; set; }

        [JsonPropertyName("tv_results")]
        public List<FindHit>? TvResults { get; set; }
    }

    private sealed class FindHit
    {
        public int Id { get; set; }
    }

    private sealed class TmdbMovieDetail
    {
        public string? Title { get; set; }

        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; set; }

        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }

        public string? Overview { get; set; }

        public string? Tagline { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        public int? Runtime { get; set; }

        public string? Status { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        public List<TmdbGenre>? Genres { get; set; }

        public TmdbCredits? Credits { get; set; }

        public TmdbVideos? Videos { get; set; }
    }

    private sealed class TmdbSeriesDetail
    {
        public string? Name { get; set; }

        [JsonPropertyName("original_name")]
        public string? OriginalName { get; set; }

        [JsonPropertyName("original_language")]
        public string? OriginalLanguage { get; set; }

        public string? Overview { get; set; }

        public string? Tagline { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }

        [JsonPropertyName("number_of_seasons")]
        public int? NumberOfSeasons { get; set; }

        [JsonPropertyName("number_of_episodes")]
        public int? NumberOfEpisodes { get; set; }

        [JsonPropertyName("episode_run_time")]
        public List<int>? EpisodeRunTime { get; set; }

        public string? Status { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        public List<TmdbGenre>? Genres { get; set; }

        public List<TmdbSeason>? Seasons { get; set; }

        [JsonPropertyName("aggregate_credits")]
        public TmdbCredits? AggregateCredits { get; set; }

        public TmdbVideos? Videos { get; set; }
    }

    private sealed class TmdbSeason
    {
        [JsonPropertyName("season_number")]
        public int SeasonNumber { get; set; }

        public string? Name { get; set; }

        public string? Overview { get; set; }

        [JsonPropertyName("episode_count")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("air_date")]
        public string? AirDate { get; set; }

        [JsonPropertyName("vote_average")]
        public decimal? VoteAverage { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }
    }

    private sealed class TmdbGenre
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }

    private sealed class TmdbCredits
    {
        public List<TmdbCast>? Cast { get; set; }

        public List<TmdbCrew>? Crew { get; set; }
    }

    private sealed class TmdbCast
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Character { get; set; }

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }

        public int? Order { get; set; }
    }

    private sealed class TmdbCrew
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Job { get; set; }

        public string? Department { get; set; }

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
    }

    private sealed class TmdbVideos
    {
        public List<TmdbVideo>? Results { get; set; }
    }

    private sealed class TmdbTranslations
    {
        public List<TmdbTranslationEntry>? Translations { get; set; }
    }

    private sealed class TmdbTranslationEntry
    {
        [JsonPropertyName("iso_639_1")]
        public string? Iso6391 { get; set; }

        public TmdbTranslationData? Data { get; set; }
    }

    private sealed class TmdbTranslationData
    {
        public string? Title { get; set; }

        public string? Name { get; set; }

        public string? Overview { get; set; }

        public string? Tagline { get; set; }
    }

    private sealed class TmdbVideo
    {
        public string? Key { get; set; }

        public string? Site { get; set; }

        public string? Type { get; set; }

        public bool Official { get; set; }
    }
}
