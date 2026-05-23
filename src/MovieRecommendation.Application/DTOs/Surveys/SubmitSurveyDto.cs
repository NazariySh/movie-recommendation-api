using System.Text.Json;

namespace MovieRecommendation.Application.DTOs.Surveys;

public class SubmitSurveyDto
{
    public int Version { get; set; }

    public JsonElement Answers { get; set; }
}
