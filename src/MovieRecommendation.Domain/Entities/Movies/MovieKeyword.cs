namespace MovieRecommendation.Domain.Entities.Movies;

public class MovieKeyword
{
    public Guid MovieId { get; set; }

    public Movie Movie { get; set; } = null!;

    public string Name { get; set; } = null!;
}
