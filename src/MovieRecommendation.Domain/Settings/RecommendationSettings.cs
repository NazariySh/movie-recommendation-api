namespace MovieRecommendation.Domain.Settings;

public class RecommendationSettings
{
    public const string SectionName = "RecommendationSettings";

    public double CfWeight { get; set; } = 0.65;

    public double SemanticWeight { get; set; } = 0.35;

    public double NegativeSignalWeight { get; set; } = 0.4;

    public int ColdStartRatingThreshold { get; set; } = 10;

    public int CandidatePoolSize { get; set; } = 200;

    public double MmrLambda { get; set; } = 0.7;

    public int BayesianMinVotes { get; set; } = 10;

    public double BayesianGlobalMean { get; set; } = 7.0;

    public double LikedThreshold { get; set; } = 8.0;

    public double DislikedThreshold { get; set; } = 4.0;
}
