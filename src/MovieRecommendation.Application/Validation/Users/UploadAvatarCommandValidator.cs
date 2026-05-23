using FluentValidation;
using MovieRecommendation.Application.Features.Users.Commands.UploadAvatar;

namespace MovieRecommendation.Application.Validation.Users;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private const long MaxBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Avatar file is empty.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("Avatar must be 2 MB or smaller.");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedTypes.Contains(ct))
            .WithMessage("Avatar must be a JPEG, PNG, or WebP image.");
    }
}
