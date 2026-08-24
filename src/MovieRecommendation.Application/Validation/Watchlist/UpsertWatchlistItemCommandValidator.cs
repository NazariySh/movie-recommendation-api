using FluentValidation;
using MovieRecommendation.Application.Features.Watchlist.Commands.UpsertWatchlistItem;

namespace MovieRecommendation.Application.Validation.Watchlist;

public class UpsertWatchlistItemCommandValidator : AbstractValidator<UpsertWatchlistItemCommand>
{
    public UpsertWatchlistItemCommandValidator()
    {
        RuleFor(x => x.Model.MovieId).NotEmpty();
        RuleFor(x => x.Model.Status).IsInEnum();
        RuleFor(x => x.Model.Notes).MaximumLength(500)
            .When(x => x.Model.Notes is not null);
    }
}
