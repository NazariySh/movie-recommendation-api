using System.Text.Json;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Features.Surveys.Commands.SubmitSurveyResponse;

public static class SurveyToPreferencesMapper
{
    private static readonly Dictionary<string, string[]> MoodToGenres = new()
    {
        ["light_funny"] = ["comedy", "family", "animation"],
        ["dark_serious"] = ["drama", "crime", "thriller"],
        ["thought_provoking"] = ["drama", "science-fiction", "documentary", "mystery"],
        ["action_intense"] = ["action", "adventure", "thriller"],
        ["romantic"] = ["romance", "drama"],
        ["scary"] = ["horror", "thriller"],
    };

    public static IReadOnlyCollection<string> CandidateGenreSlugs { get; } =
        MoodToGenres.Values.SelectMany(s => s).Distinct().ToArray();

    public static IReadOnlyList<UserGenrePreference> Map(
        Guid userId,
        JsonElement answers,
        IReadOnlyDictionary<string, Genre> genresBySlug)
    {
        var moodScores = ExtractMoodScores(answers);

        var perGenreTotal = new Dictionary<int, decimal>();
        var perGenreCount = new Dictionary<int, int>();

        foreach (var (mood, raw) in moodScores)
        {
            if (!MoodToGenres.TryGetValue(mood, out var slugs))
            {
                continue;
            }

            var normalised = Math.Max(0m, (decimal)(raw - 1) / 4m);

            foreach (var slug in slugs)
            {
                if (!genresBySlug.TryGetValue(slug, out var genre))
                {
                    continue;
                }

                perGenreTotal[genre.Id] = perGenreTotal.GetValueOrDefault(genre.Id) + normalised;
                perGenreCount[genre.Id] = perGenreCount.GetValueOrDefault(genre.Id) + 1;
            }
        }

        var now = DateTime.UtcNow;
        return perGenreTotal
            .Select(kv => new UserGenrePreference
            {
                UserId = userId,
                GenreId = kv.Key,
                Weight = perGenreCount[kv.Key] > 0 ? kv.Value / perGenreCount[kv.Key] : 0m,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();
    }

    private static Dictionary<string, int> ExtractMoodScores(JsonElement answers)
    {
        var result = new Dictionary<string, int>();

        if (answers.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!answers.TryGetProperty("mood_preferences", out var moods))
        {
            return result;
        }

        if (moods.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var prop in moods.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var score))
            {
                result[prop.Name] = score;
            }
        }

        return result;
    }
}
