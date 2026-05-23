namespace MovieRecommendation.Application.DTOs.Surveys;

public class SurveyQuestionDto
{
    public string Id { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Question { get; set; }

    public IReadOnlyList<SurveyOptionDto>? Options { get; set; }

    public IReadOnlyList<string>? Rows { get; set; }

    public SurveyScaleDto? Scale { get; set; }

    public int? Max { get; set; }

    public int? MinRequired { get; set; }

    public string? MoviePool { get; set; }

    public bool Required { get; set; }
}
