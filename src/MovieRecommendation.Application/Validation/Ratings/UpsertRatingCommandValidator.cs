using FluentValidation;
using MovieRecommendation.Application.Features.Ratings.Commands.UpsertRating;

namespace MovieRecommendation.Application.Validation.Ratings;

public class UpsertRatingCommandValidator : AbstractValidator<UpsertRatingCommand>
{
    public UpsertRatingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.MovieId).NotEmpty();
        RuleFor(x => x.Model.Value)
            .InclusiveBetween(1m, 10m)
            .WithMessage("Rating must be between 1 and 10.");
    }
}
