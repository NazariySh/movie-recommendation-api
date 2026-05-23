using FluentValidation;
using MovieRecommendation.Application.Features.Movies.Commands.ImportMovieFromImdb;

namespace MovieRecommendation.Application.Validation.Movies;

public class ImportMovieFromImdbCommandValidator : AbstractValidator<ImportMovieFromImdbCommand>
{
    public ImportMovieFromImdbCommandValidator()
    {
        RuleFor(x => x.Model.ImdbId)
            .NotEmpty()
            .Matches(@"^tt\d{7,10}$")
            .WithMessage("ImdbId must match pattern tt\\d{7,10}.");
    }
}
