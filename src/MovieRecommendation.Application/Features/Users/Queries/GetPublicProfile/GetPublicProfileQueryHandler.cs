using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Users.Queries.GetPublicProfile;

public class GetPublicProfileQueryHandler : IQueryHandler<GetPublicProfileQuery, PublicProfileDto>
{
    private readonly IUserRepository _userRepository;

    public GetPublicProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PublicProfileDto> Handle(GetPublicProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _userRepository.GetPublicProfileAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException($"User {request.UserId} not found");
        }

        return profile;
    }
}
