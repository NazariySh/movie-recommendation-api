namespace MovieRecommendation.Domain.Entities.Movies;

public class Person : BaseEntity, ISoftDeletable
{
    public int? TmdbId { get; set; }

    public string? ImdbId { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public DateOnly? Birthday { get; set; }

    public DateOnly? DateOfDeath { get; set; }

    public string? PlaceOfBirth { get; set; }

    public string? Nationality { get; set; }

    public string? Gender { get; set; }

    public string? KnownForDepartment { get; set; }

    public string? Biography { get; set; }

    public bool IsDeleted { get; set; }
}
