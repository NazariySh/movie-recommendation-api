using FluentValidation;
using MovieRecommendation.Application.Features.Users.Commands.ChangePassword;
using MovieRecommendation.Application.Validation.Auth;

namespace MovieRecommendation.Application.Validation.Users;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Model.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.Model.NewPassword).MustBeStrongPassword();

        RuleFor(x => x.Model)
            .Must(m => m.CurrentPassword != m.NewPassword)
            .WithMessage("New password must differ from current password.");
    }
}
