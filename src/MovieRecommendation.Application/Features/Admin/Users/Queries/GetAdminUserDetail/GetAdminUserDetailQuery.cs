using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Admin;

namespace MovieRecommendation.Application.Features.Admin.Users.Queries.GetAdminUserDetail;

public record GetAdminUserDetailQuery(Guid UserId) : IQuery<AdminUserDetailDto>;
