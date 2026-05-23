using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.ForgotPassword;

namespace MovieRecommendation.Application.Validation.Auth;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Model.Email).NotEmpty().EmailAddress();
    }
}
