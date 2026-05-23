using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class SurveyResponseRepository : BaseRepository<SurveyResponse>, ISurveyResponseRepository
{
    public SurveyResponseRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }

    public Task<SurveyResponse?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return DbContext.SurveyResponses
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsDeleted)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
