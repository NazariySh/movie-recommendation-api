using System.Text.Json;

namespace MovieRecommendation.Application.DTOs.Surveys;

public class SurveyResponseDto
{
    public int Version { get; set; }

    public DateTime CompletedAt { get; set; }

    public JsonElement Answers { get; set; }
}
