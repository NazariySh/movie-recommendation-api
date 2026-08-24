using FluentValidation;
using MovieRecommendation.Application.Features.Search.Queries.GetSearchSuggestions;

namespace MovieRecommendation.Application.Validation.Search;

public class GetSearchSuggestionsQueryValidator : AbstractValidator<GetSearchSuggestionsQuery>
{
    public GetSearchSuggestionsQueryValidator()
    {
        RuleFor(x => x.Query).MaximumLength(200);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(8);
        RuleFor(x => x.Limit).InclusiveBetween(1, 20);
    }
}
