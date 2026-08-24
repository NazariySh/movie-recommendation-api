using System.Text.Json;
using FluentValidation;
using MovieRecommendation.Application.Features.Surveys.Commands.SubmitSurveyResponse;

namespace MovieRecommendation.Application.Validation.Users;

public class SubmitSurveyResponseCommandValidator : AbstractValidator<SubmitSurveyResponseCommand>
{
    public SubmitSurveyResponseCommandValidator()
    {
        RuleFor(x => x.Request.Version).GreaterThan(0);

        RuleFor(x => x.Request.Answers)
            .Must(a => a.ValueKind == JsonValueKind.Object)
            .WithMessage("Answers payload must be a JSON object keyed by question id.");
    }
}
