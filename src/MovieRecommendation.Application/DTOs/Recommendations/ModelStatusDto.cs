namespace MovieRecommendation.Application.DTOs.Recommendations;

public class ModelStatusDto
{
    public string? Version { get; set; }

    public DateTime? TrainedAt { get; set; }

    public double? Rmse { get; set; }

    public double? R2 { get; set; }

    public int? SampleCount { get; set; }

    public bool IsActive { get; set; }

    public bool HasModel => Version is not null;
}
