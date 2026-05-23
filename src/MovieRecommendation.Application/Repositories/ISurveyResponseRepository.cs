using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.Application.Repositories;

public interface ISurveyResponseRepository : IRepository<SurveyResponse>
{
    Task<SurveyResponse?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
