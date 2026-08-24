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

        RuleFor(x => x.Model.PlaceOfBirth).MaximumLength(255)
            .When(x => x.Model.PlaceOfBirth is not null);

        RuleFor(x => x.Model.Nationality).MaximumLength(100)
            .When(x => x.Model.Nationality is not null);

        RuleFor(x => x.Model.Gender).MaximumLength(20)
            .When(x => x.Model.Gender is not null);

        RuleFor(x => x.Model.KnownForDepartment).MaximumLength(50)
            .When(x => x.Model.KnownForDepartment is not null);

        RuleFor(x => x.Model.ImdbId)
            .Matches(@"^nm\d+$").WithMessage("ImdbId must match pattern nm\\d+.")
            .When(x => !string.IsNullOrWhiteSpace(x.Model.ImdbId));

        RuleFor(x => x.Model)
            .Must(m => !m.DateOfDeath.HasValue || !m.Birthday.HasValue || m.DateOfDeath > m.Birthday)
            .WithMessage("Date of death must be after birthday.");
    }
}
