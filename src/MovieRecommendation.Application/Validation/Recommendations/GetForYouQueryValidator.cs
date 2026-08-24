using FluentValidation;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetForYou;

namespace MovieRecommendation.Application.Validation.Recommendations;

public class GetForYouQueryValidator : AbstractValidator<GetForYouQuery>
{
    public GetForYouQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
    }
}
