using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.VerifyEmail;

namespace MovieRecommendation.Application.Validation.Auth;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Model.UserId).NotEmpty();
        RuleFor(x => x.Model.Token).NotEmpty();
    }
}
