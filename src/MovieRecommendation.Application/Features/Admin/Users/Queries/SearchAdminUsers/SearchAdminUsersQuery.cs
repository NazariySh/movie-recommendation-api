using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Admin.Users.Queries.SearchAdminUsers;

public record SearchAdminUsersQuery(SearchAdminUsersDto Query) : IQuery<PagedList<AdminUserListItemDto>>;
