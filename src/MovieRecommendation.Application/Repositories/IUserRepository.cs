using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Repositories;

public interface IUserRepository
{
    Task<TProjection?> GetByIdAsync<TProjection>(Guid userId, CancellationToken ct = default)
        where TProjection : class;

    Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default);

    Task<bool> IsUsernameUniqueAsync(string username, CancellationToken ct = default);

    Task<PublicProfileDto?> GetPublicProfileAsync(Guid userId, CancellationToken ct = default);

    Task<UserStatsDto?> GetStatsAsync(Guid userId, string lang, CancellationToken ct = default);

    Task<PagedList<AdminUserListItemDto>> SearchAdminAsync(SearchAdminUsersDto query, CancellationToken ct = default);

    Task<AdminUserDetailDto?> GetAdminDetailAsync(Guid userId, CancellationToken ct = default);
}
