using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Surveys;

namespace MovieRecommendation.Application.Features.Surveys.Queries.GetCurrentSurvey;

public record GetCurrentSurveyQuery(string Lang) : IQuery<SurveyDto>;
