using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Queries.GetMyStats;

public record GetMyStatsQuery(Guid UserId, string Lang) : IQuery<UserStatsDto>;
