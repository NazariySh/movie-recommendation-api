namespace MovieRecommendation.Application.DTOs.Surveys;

public class SurveyDto
{
    public int Version { get; set; }

    public IReadOnlyList<SurveyQuestionDto> Questions { get; set; } = [];
}
