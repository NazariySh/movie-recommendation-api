using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Surveys;

namespace MovieRecommendation.Application.Features.Surveys.Queries.GetMyLatestResponse;

public record GetMyLatestSurveyResponseQuery(Guid UserId) : IQuery<SurveyResponseDto?>;
