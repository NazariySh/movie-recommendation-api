using Microsoft.ML.Data;

namespace MovieRecommendation.ML.Models;

public class MovieRatingData
{
    [LoadColumn(0)]
    public string UserId { get; set; } = null!;

    [LoadColumn(1)]
    public string MovieId { get; set; } = null!;

    [LoadColumn(2)]
    [ColumnName("Label")]
    public float Label { get; set; }
}
