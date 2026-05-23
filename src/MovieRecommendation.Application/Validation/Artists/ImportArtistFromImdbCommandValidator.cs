using FluentValidation;
using MovieRecommendation.Application.Features.Artists.Commands.ImportArtistFromImdb;

namespace MovieRecommendation.Application.Validation.Artists;

public class ImportArtistFromImdbCommandValidator : AbstractValidator<ImportArtistFromImdbCommand>
{
    public ImportArtistFromImdbCommandValidator()
    {
        RuleFor(x => x.Model.ImdbId)
            .NotEmpty()
            .Matches(@"^nm\d+$")
            .WithMessage("ImdbId must match pattern nm\\d+.");
    }
}
