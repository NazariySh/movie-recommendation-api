using FluentValidation;
using MovieRecommendation.Application.Features.Auth.Commands.GoogleAuth;

namespace MovieRecommendation.Application.Validation.Auth;

public class GoogleAuthCommandValidator : AbstractValidator<GoogleAuthCommand>
{
    public GoogleAuthCommandValidator()
    {
        RuleFor(x => x.Model.IdToken).NotEmpty();
    }
}
