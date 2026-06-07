using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Admin.Users.Queries.SearchAdminUsers;

public class SearchAdminUsersQueryHandler : IQueryHandler<SearchAdminUsersQuery, PagedList<AdminUserListItemDto>>
{
    private readonly IUserRepository _userRepository;

    public SearchAdminUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedList<AdminUserListItemDto>> Handle(SearchAdminUsersQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.SearchAdminAsync(request.Query, cancellationToken);
    }
}
