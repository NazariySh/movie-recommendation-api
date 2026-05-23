using FluentValidation;
using MovieRecommendation.Application.Features.Reviews.Commands.CreateReply;

namespace MovieRecommendation.Application.Validation.Reviews;

public class CreateReplyCommandValidator : AbstractValidator<CreateReplyCommand>
{
    public CreateReplyCommandValidator()
    {
        RuleFor(x => x.ParentReviewId).NotEmpty();

        RuleFor(x => x.Model.Body)
            .NotEmpty()
            .Length(1, 1000);

        RuleFor(x => x.Model.Score)
            .Null()
            .WithMessage("Replies cannot carry a score — only top-level reviews can be scored.");
    }
}
