using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace MovieRecommendation.Infrastructure.Data.Seeding.MovieLens;

public sealed class MovieLensCsvReader : IMovieLensCsvReader
{
    private const int MinYear = 1888;
    private const int MaxYear = 2030;
    private const int ImdbIdMinDigits = 7;
    private const string NoGenresMarker = "(no genres listed)";

    private static readonly Regex YearTrailingRegex =
        new(@"^(?<title>.+?)\s*\((?<year>\d{4})\)\s*$", RegexOptions.Compiled);

    private static readonly CsvConfiguration CsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        IgnoreBlankLines = true,
        TrimOptions = TrimOptions.Trim,
        BadDataFound = null,
        MissingFieldFound = null,
    };

    public IReadOnlyList<MovieLensRecord> Read(string moviesPath, string linksPath)
    {
        var links = ReadLinks(linksPath);

        using var reader = new StreamReader(moviesPath);
        using var csv = new CsvReader(reader, CsvConfiguration);

        return csv.GetRecords<MovieLensMovieRow>()
            .Select(row => ToRecord(row, links))
            .Where(record => record is not null)
            .Select(record => record!)
            .ToList();
    }

    private static MovieLensRecord? ToRecord(MovieLensMovieRow row, IReadOnlyDictionary<int, MovieLink> links)
    {
        var (title, year) = ExtractTitleAndYear(row.Title?.Trim('"').Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var genres = (row.Genres ?? string.Empty)
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Where(g => !string.Equals(g, NoGenresMarker, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var link = links.TryGetValue(row.MovieId, out var l) ? l : MovieLink.Empty;

        return new MovieLensRecord(row.MovieId, title, year, link.ImdbId, link.TmdbId, genres);
    }

    private static IReadOnlyDictionary<int, MovieLink> ReadLinks(string linksPath)
    {
        if (!File.Exists(linksPath))
        {
            return new Dictionary<int, MovieLink>();
        }

        using var reader = new StreamReader(linksPath);
        using var csv = new CsvReader(reader, CsvConfiguration);

        return csv.GetRecords<MovieLensLinkRow>()
            .ToDictionary(
                row => row.MovieId,
                row => new MovieLink(FormatImdbId(row.ImdbId), row.TmdbId));
    }

    private static string? FormatImdbId(string? raw)
    {
        var trimmed = raw?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : $"tt{trimmed.PadLeft(ImdbIdMinDigits, '0')}";
    }

    private static (string Title, int? Year) ExtractTitleAndYear(string raw)
    {
        var match = YearTrailingRegex.Match(raw);
        if (!match.Success)
        {
            return (raw.Trim(), null);
        }

        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        if (year < MinYear || year > MaxYear)
        {
            return (raw.Trim(), null);
        }

        return (match.Groups["title"].Value.Trim(), year);
    }

    private sealed class MovieLensMovieRow
    {
        [Name("movieId")]
        public int MovieId { get; set; }

        [Name("title")]
        public string? Title { get; set; }

        [Name("genres")]
        public string? Genres { get; set; }
    }

    private sealed class MovieLensLinkRow
    {
        [Name("movieId")]
        public int MovieId { get; set; }

        [Name("imdbId")]
        public string? ImdbId { get; set; }

        [Name("tmdbId")]
        public int? TmdbId { get; set; }
    }

    private sealed record MovieLink(string? ImdbId, int? TmdbId)
    {
        public static readonly MovieLink Empty = new(null, null);
    }
}
