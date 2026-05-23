using FluentValidation;
using MovieRecommendation.Application.Features.Reviews.Commands.CreateReview;

namespace MovieRecommendation.Application.Validation.Reviews;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.MovieId).NotEmpty();

        RuleFor(x => x.Model.Body)
            .NotEmpty()
            .Length(1, 5000);

        RuleFor(x => x.Model.Score)
            .InclusiveBetween(1m, 10m)
            .When(x => x.Model.Score.HasValue);
    }
}
