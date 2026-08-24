using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Queries.GetPublicProfile;

public record GetPublicProfileQuery(Guid UserId) : IQuery<PublicProfileDto>;
