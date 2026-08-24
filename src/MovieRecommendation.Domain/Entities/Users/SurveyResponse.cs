namespace MovieRecommendation.Domain.Entities.Users;

public class SurveyResponse : BaseEntity, ISoftDeletable
{
    public string Answers { get; set; } = "{}";

    public int Version { get; set; }

    public DateTime CompletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
