using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.Register;
using MovieRecommendation.Domain.Constants;

namespace MovieRecommendation.Application.Validation.Auth;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Model.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Username must be at most 30 characters.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username may contain letters, digits and underscore only.");

        RuleFor(x => x.Model.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255);

        RuleFor(x => x.Model.Password).MustBeStrongPassword();

        RuleFor(x => x.Model.PreferredLanguage)
            .NotEmpty()
            .Must(LanguageCodes.SupportedLanguages.Contains)
            .WithMessage($"Preferred language must be '{LanguageCodes.Ukrainian}' or '{LanguageCodes.English}'.");
    }
}
