using FluentValidation;
using MovieRecommendation.Application.Features.Genres.Commands.CreateGenre;

namespace MovieRecommendation.Application.Validation.Genres;

public class CreateGenreCommandValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreCommandValidator()
    {
        RuleFor(x => x.Model.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Model.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.LanguageCode).NotEmpty().Length(2, 10);
            t.RuleFor(x => x.Name).NotEmpty().Length(1, 100);
        });
    }
}
