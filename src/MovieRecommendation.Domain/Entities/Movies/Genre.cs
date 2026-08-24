namespace MovieRecommendation.Domain.Entities.Movies;

public class Genre : BaseEntity<int>, ISoftDeletable
{
    public string Slug { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public ICollection<GenreTranslation> Translations { get; set; } = [];
}
