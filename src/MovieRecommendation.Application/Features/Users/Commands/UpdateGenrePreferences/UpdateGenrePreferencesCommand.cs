using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Users;

namespace MovieRecommendation.Application.Features.Users.Commands.UpdateGenrePreferences;

public record UpdateGenrePreferencesCommand(
    Guid UserId,
    IReadOnlyList<UpdateGenrePreferenceDto> Preferences,
    string Lang) : ICommand<IReadOnlyList<GenrePreferenceDto>>;
