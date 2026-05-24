using FluentValidation;
using MovieRecommendation.Application.Features.WatchHistory.Commands.AddWatchHistory;

namespace MovieRecommendation.Application.Validation.Watchlist;

public class AddWatchHistoryCommandValidator : AbstractValidator<AddWatchHistoryCommand>
{
    public AddWatchHistoryCommandValidator()
    {
        RuleFor(x => x.Model.MovieId).NotEmpty();
        RuleFor(x => x.Model.WatchedAt)
            .Must(d => !d.HasValue || d.Value <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("WatchedAt cannot be in the future.");
    }
}
