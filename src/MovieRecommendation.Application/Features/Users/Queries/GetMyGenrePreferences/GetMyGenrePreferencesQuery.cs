using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Queries.GetMyGenrePreferences;

public record GetMyGenrePreferencesQuery(Guid UserId, string Lang) : IQuery<IReadOnlyList<GenrePreferenceDto>>;
