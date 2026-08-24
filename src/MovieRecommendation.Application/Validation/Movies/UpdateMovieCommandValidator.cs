using FluentValidation;
using MovieRecommendation.Application.Features.Movies.Commands.UpdateMovie;

namespace MovieRecommendation.Application.Validation.Movies;

public class UpdateMovieCommandValidator : AbstractValidator<UpdateMovieCommand>
{
    public UpdateMovieCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Model.OriginalTitle)
            .NotEmpty()
            .Length(1, 200);

        RuleFor(x => x.Model.OriginalLang)
            .NotEmpty()
            .Length(2, 10);

        RuleFor(x => x.Model.Runtime)
            .GreaterThan(0).When(x => x.Model.Runtime.HasValue);

        RuleFor(x => x.Model.ImdbId)
            .Matches(@"^tt\d{7,10}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Model.ImdbId));
    }
}
