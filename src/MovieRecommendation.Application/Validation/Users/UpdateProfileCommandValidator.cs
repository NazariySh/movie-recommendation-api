using FluentValidation;
using MovieRecommendation.Application.Features.Users.Commands.UpdateProfile;

namespace MovieRecommendation.Application.Validation.Users;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Request.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(30).WithMessage("Username must be at most 30 characters.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username may contain letters, digits and underscore only.");

        RuleFor(x => x.Request.Bio)
            .MaximumLength(500).WithMessage("Bio must be at most 500 characters.");

        RuleFor(x => x.Request.PreferredLanguage)
            .NotEmpty()
            .Must(l => l == "uk" || l == "en")
            .WithMessage("Preferred language must be 'uk' or 'en'.");
    }
}
