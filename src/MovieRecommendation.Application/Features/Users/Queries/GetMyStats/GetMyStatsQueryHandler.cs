using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Queries.GetMyStats;

public class GetMyStatsQueryHandler : IQueryHandler<GetMyStatsQuery, UserStatsDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cache;

    public GetMyStatsQueryHandler(IUserRepository userRepository, ICacheService cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }

    public Task<UserStatsDto> Handle(GetMyStatsQuery request, CancellationToken cancellationToken)
    {
        var key = UserCacheKeys.Stats(request.UserId, request.Lang);
        return _cache.GetOrSetAsync(
            key,
            async (token) =>
            {
                var user = await _userRepository.GetStatsAsync(request.UserId, request.Lang, token);
                if (user is null)
                {
                    throw new NotFoundException($"User {request.UserId} not found");
                }

                return user;
            },
            CacheTtl,
            cancellationToken);
    }
}
