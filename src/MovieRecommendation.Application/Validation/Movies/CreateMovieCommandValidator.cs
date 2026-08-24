using FluentValidation;
using MovieRecommendation.Application.Features.Movies.Commands.CreateMovie;

namespace MovieRecommendation.Application.Validation.Movies;

public class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieCommandValidator()
    {
        RuleFor(x => x.Model.OriginalTitle)
            .NotEmpty()
            .Length(1, 200);

        RuleFor(x => x.Model.OriginalLang)
            .NotEmpty()
            .Length(2, 10);

        RuleFor(x => x.Model.Runtime)
            .GreaterThan(0).When(x => x.Model.Runtime.HasValue);

        RuleFor(x => x.Model.ReleaseDate)
            .Must(d => !d.HasValue || d.Value <= DateTime.UtcNow.AddYears(5))
            .WithMessage("Release date cannot be more than 5 years in the future.");

        RuleFor(x => x.Model.ImdbId)
            .Matches(@"^tt\d{7,10}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Model.ImdbId))
            .WithMessage("ImdbId must match pattern tt\\d{7,10}.");

        RuleForEach(x => x.Model.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.LanguageCode).NotEmpty().Length(2, 10);
            t.RuleFor(x => x.Title).NotEmpty().Length(1, 200);
        });
    }
}
