namespace MovieRecommendation.Application.DTOs.Movies;

public class SeasonDto
{
    public Guid Id { get; set; }

    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public string? Overview { get; set; }

    public string? PosterUrl { get; set; }

    public int EpisodeCount { get; set; }

    public DateTime? AirDate { get; set; }

    public decimal? VoteAverage { get; set; }

    public decimal AverageRating { get; set; }

    public int RatingsCount { get; set; }

    public decimal? MyRating { get; set; }
}
