using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Queries.GetMyProfile;

public record GetMyProfileQuery(Guid UserId) : IQuery<UserProfileDto>;
