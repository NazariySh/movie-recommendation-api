using MovieRecommendation.Application.Abstractions.Time;

namespace MovieRecommendation.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
