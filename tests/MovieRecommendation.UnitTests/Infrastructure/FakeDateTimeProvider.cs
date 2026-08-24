using MovieRecommendation.Application.Abstractions.Time;

namespace MovieRecommendation.UnitTests.Infrastructure;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider()
        : this(new DateTime(2026, 5, 16, 12, 0, 0, DateTimeKind.Utc))
    {
    }

    public FakeDateTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }

    public void Advance(TimeSpan by)
    {
        UtcNow = UtcNow.Add(by);
    }
}
