using FluentValidation;
using MovieRecommendation.Application.Features.Reviews.Commands.UpdateReview;

namespace MovieRecommendation.Application.Validation.Reviews;

public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Model.Body)
            .NotEmpty()
            .Length(5, 5000);

        RuleFor(x => x.Model.Score)
            .InclusiveBetween(1m, 10m)
            .When(x => x.Model.Score.HasValue);
    }
}
