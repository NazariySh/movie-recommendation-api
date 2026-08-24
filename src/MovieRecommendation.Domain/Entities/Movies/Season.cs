namespace MovieRecommendation.Domain.Entities.Movies;

public class Season : BaseEntity, ISoftDeletable
{
    public Guid MovieId { get; set; }

    public int SeasonNumber { get; set; }

    public string? Name { get; set; }

    public string? Overview { get; set; }

    public string? PosterUrl { get; set; }

    public int EpisodeCount { get; set; }

    public DateTime? AirDate { get; set; }

    public decimal? VoteAverage { get; set; }

    public bool IsDeleted { get; set; }

    public Movie Movie { get; set; } = null!;
}
