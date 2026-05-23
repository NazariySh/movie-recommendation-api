using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Surveys;

namespace MovieRecommendation.Application.Features.Surveys.Commands.SubmitSurveyResponse;

public record SubmitSurveyResponseCommand(Guid UserId, SubmitSurveyDto Request) : ICommand;
