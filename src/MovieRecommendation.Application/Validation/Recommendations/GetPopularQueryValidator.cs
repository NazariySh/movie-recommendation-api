using FluentValidation;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetPopular;

namespace MovieRecommendation.Application.Validation.Recommendations;

public class GetPopularQueryValidator : AbstractValidator<GetPopularQuery>
{
    public GetPopularQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
        RuleFor(x => x.GenreSlug)
            .MaximumLength(64)
            .When(x => x.GenreSlug is not null);
    }
}
