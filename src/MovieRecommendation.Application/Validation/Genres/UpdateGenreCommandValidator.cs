using FluentValidation;
using MovieRecommendation.Application.Features.Genres.Commands.UpdateGenre;

namespace MovieRecommendation.Application.Validation.Genres;

public class UpdateGenreCommandValidator : AbstractValidator<UpdateGenreCommand>
{
    public UpdateGenreCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.Model.Slug).NotEmpty();

        RuleForEach(x => x.Model.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.LanguageCode).NotEmpty().Length(2, 10);
            t.RuleFor(x => x.Name).NotEmpty().Length(1, 100);
        });
    }
}
