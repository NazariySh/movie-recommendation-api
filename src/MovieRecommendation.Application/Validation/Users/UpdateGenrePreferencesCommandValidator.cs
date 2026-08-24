using FluentValidation;
using MovieRecommendation.Application.Features.Users.Commands.UpdateGenrePreferences;

namespace MovieRecommendation.Application.Validation.Users;

public class UpdateGenrePreferencesCommandValidator : AbstractValidator<UpdateGenrePreferencesCommand>
{
    public UpdateGenrePreferencesCommandValidator()
    {
        RuleFor(x => x.Preferences)
            .NotNull()
            .Must(p => p.Count <= 30).WithMessage("At most 30 genre preferences are allowed.");

        RuleForEach(x => x.Preferences).ChildRules(p =>
        {
            p.RuleFor(x => x.GenreId).GreaterThan(0);
            p.RuleFor(x => x.Weight)
                .InclusiveBetween(0m, 1m)
                .WithMessage("Genre weight must be between 0.0 and 1.0.");
        });
    }
}
