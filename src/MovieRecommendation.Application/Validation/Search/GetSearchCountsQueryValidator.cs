using FluentValidation;
using MovieRecommendation.Application.Features.Search.Queries.GetSearchCounts;

namespace MovieRecommendation.Application.Validation.Search;

public class GetSearchCountsQueryValidator : AbstractValidator<GetSearchCountsQuery>
{
    public GetSearchCountsQueryValidator()
    {
        RuleFor(x => x.Query).MaximumLength(200);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
    }
}
