using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.ResendVerification;

namespace MovieRecommendation.Application.Validation.Auth;

public class ResendVerificationCommandValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationCommandValidator()
    {
        RuleFor(x => x.Model.Email).NotEmpty().EmailAddress();
    }
}
