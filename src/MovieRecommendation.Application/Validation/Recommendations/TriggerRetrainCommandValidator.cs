using FluentValidation;
using MovieRecommendation.Application.Features.Recommendations.Commands.TriggerRetrain;

namespace MovieRecommendation.Application.Validation.Recommendations;

public class TriggerRetrainCommandValidator : AbstractValidator<TriggerRetrainCommand>
{
    public TriggerRetrainCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
