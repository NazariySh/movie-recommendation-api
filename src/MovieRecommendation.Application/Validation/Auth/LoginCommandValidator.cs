using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.Login;

namespace MovieRecommendation.Application.Validation.Auth;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Model.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Model.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
