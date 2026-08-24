using FluentValidation;
using MovieRecommendation.Application.Features.Watchlist.Commands.UpdateWatchlistStatus;

namespace MovieRecommendation.Application.Validation.Watchlist;

public class UpdateWatchlistStatusCommandValidator : AbstractValidator<UpdateWatchlistStatusCommand>
{
    public UpdateWatchlistStatusCommandValidator()
    {
        RuleFor(x => x.MovieId).NotEmpty();
        RuleFor(x => x.Model.Status).IsInEnum();
    }
}
