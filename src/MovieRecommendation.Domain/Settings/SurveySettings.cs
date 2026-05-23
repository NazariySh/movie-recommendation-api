namespace MovieRecommendation.Domain.Settings;

public class SurveySettings
{
    public const string SectionName = "Surveys";

    public int Version { get; set; }

    public List<SurveyQuestionSettings> Questions { get; set; } = [];
}
public class SurveyQuestionSettings
{
    public string Id { get; set; } = null!;

    public string Type { get; set; } = null!;

    public Dictionary<string, string> Question { get; set; } = [];

    public List<SurveyOptionSettings>? Options { get; set; }

    public List<string>? Rows { get; set; }

    public SurveyScaleSettings? Scale { get; set; }

    public int? Max { get; set; }

    public int? MinRequired { get; set; }

    public string? MoviePool { get; set; }

    public bool Required { get; set; }
}

public class SurveyOptionSettings
{
    public string Id { get; set; } = null!;

    public Dictionary<string, string> Label { get; set; } = [];
}

public class SurveyScaleSettings
{
    public int Min { get; set; } = 1;

    public int Max { get; set; } = 5;
}
