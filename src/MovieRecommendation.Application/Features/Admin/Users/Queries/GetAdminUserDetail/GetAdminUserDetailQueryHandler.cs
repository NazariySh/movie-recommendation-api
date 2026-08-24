using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Admin;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Admin.Users.Queries.GetAdminUserDetail;

public class GetAdminUserDetailQueryHandler : IQueryHandler<GetAdminUserDetailQuery, AdminUserDetailDto>
{
    private readonly IUserRepository _userRepository;

    public GetAdminUserDetailQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AdminUserDetailDto> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken)
    {
        var detail = await _userRepository.GetAdminDetailAsync(request.UserId, cancellationToken);
        if (detail is null)
        {
            throw new NotFoundException($"User {request.UserId} not found.");
        }

        return detail;
    }
}
