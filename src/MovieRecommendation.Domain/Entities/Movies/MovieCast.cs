namespace MovieRecommendation.Domain.Entities.Movies;

public class MovieCast
{
    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public Guid PersonId { get; set; }

    public Person Person { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? Character { get; set; }

    public int? CastOrder { get; set; }
}
