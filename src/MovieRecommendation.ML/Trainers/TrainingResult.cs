namespace MovieRecommendation.ML.Trainers;

public record TrainingResult(
    string Version,
    double Rmse,
    double R2,
    int SampleCount,
    DateTime TrainedAt);
