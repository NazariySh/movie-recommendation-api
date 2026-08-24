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

    public int MinRatingsToTrain { get; set; } = 100;

    public int MaxPredictionsPerUser { get; set; } = 200;

    public int PredictionInsertBatchSize { get; set; } = 5000;

    public int RetainPredictionVersions { get; set; } = 2;

    public int RetrainingIntervalDays { get; set; } = 7;

    public int InitialTrainingDelaySeconds { get; set; } = 15;

    public string ModelStoragePath { get; set; } = "MLModels";

    public int MatrixFactorizationIterations { get; set; } = 20;

    public int MatrixFactorizationRank { get; set; } = 100;

    public double MatrixFactorizationLearningRate { get; set; } = 0.01;

    public double MatrixFactorizationLambda { get; set; } = 0.025;
}
