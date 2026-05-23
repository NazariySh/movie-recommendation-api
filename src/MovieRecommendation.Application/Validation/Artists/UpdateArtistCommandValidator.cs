using FluentValidation;
using MovieRecommendation.Application.Features.Artists.Commands.UpdateArtist;

namespace MovieRecommendation.Application.Validation.Artists;

public class UpdateArtistCommandValidator : AbstractValidator<UpdateArtistCommand>
{
    public UpdateArtistCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Model.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(255);

        RuleFor(x => x.Model.PhotoUrl).MaximumLength(2000)
            .When(x => x.Model.PhotoUrl is not null);

        RuleFor(x => x.Model.Biography).MaximumLength(4000)
            .When(x => x.Model.Biography is not null);

        RuleFor(x => x.Model)
            .Must(m => !m.DateOfDeath.HasValue || !m.Birthday.HasValue || m.DateOfDeath > m.Birthday)
            .WithMessage("Date of death must be after birthday.");
    }
}
