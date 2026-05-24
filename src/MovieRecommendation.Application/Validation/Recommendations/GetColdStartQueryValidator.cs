using FluentValidation;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetColdStart;

namespace MovieRecommendation.Application.Validation.Recommendations;

public class GetColdStartQueryValidator : AbstractValidator<GetColdStartQuery>
{
    public GetColdStartQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
    }
}
