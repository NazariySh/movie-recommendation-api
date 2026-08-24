using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Recommendations;

namespace MovieRecommendation.Application.Features.Recommendations.Queries.GetModelStatus;

public record GetModelStatusQuery : IQuery<ModelStatusDto>;
