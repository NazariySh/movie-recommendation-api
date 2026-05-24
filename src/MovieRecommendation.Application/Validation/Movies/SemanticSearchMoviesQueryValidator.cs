using FluentValidation;
using MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

namespace MovieRecommendation.Application.Validation.Movies;

public class SemanticSearchMoviesQueryValidator : AbstractValidator<SemanticSearchMoviesQuery>
{
    public SemanticSearchMoviesQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Semantic search query must be 1-500 characters.");
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.MinScore).InclusiveBetween(0.0, 1.0);
    }
}
