namespace MovieRecommendation.Domain.Entities.Predictions;

public class MlModelMetadata : BaseEntity, ISoftDeletable
{
    public string Version { get; set; } = null!;

    public double Rmse { get; set; }

    public double R2 { get; set; }

    public int SampleCount { get; set; }

    public DateTime TrainedAt { get; set; }

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }
}
