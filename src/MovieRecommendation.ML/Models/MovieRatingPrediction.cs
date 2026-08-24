using Microsoft.ML.Data;

namespace MovieRecommendation.ML.Models;

public class MovieRatingPrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}
