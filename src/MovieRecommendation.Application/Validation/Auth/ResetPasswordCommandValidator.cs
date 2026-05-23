using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.ResetPassword;

namespace MovieRecommendation.Application.Validation.Auth;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Model.UserId).NotEmpty();
        RuleFor(x => x.Model.Token).NotEmpty();
        RuleFor(x => x.Model.NewPassword).MustBeStrongPassword();
    }
}
