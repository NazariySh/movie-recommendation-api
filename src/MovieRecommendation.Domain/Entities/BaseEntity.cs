namespace MovieRecommendation.Domain.Entities;

public class BaseEntity<TKey> : IAuditable
{
    public TKey Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class BaseEntity : BaseEntity<Guid>;
