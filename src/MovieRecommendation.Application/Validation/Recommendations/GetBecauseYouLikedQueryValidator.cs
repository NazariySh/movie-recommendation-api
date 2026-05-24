using FluentValidation;
using MovieRecommendation.Application.Features.Recommendations.Queries.GetBecauseYouLiked;

namespace MovieRecommendation.Application.Validation.Recommendations;

public class GetBecauseYouLikedQueryValidator : AbstractValidator<GetBecauseYouLikedQuery>
{
    public GetBecauseYouLikedQueryValidator()
    {
        RuleFor(x => x.MovieId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 50);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
    }
}
