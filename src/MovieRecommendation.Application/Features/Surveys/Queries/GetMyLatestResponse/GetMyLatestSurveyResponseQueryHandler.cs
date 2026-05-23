using System.Text.Json;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Surveys;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Surveys.Queries.GetMyLatestResponse;

public class GetMyLatestSurveyResponseQueryHandler : IQueryHandler<GetMyLatestSurveyResponseQuery, SurveyResponseDto?>
{
    private readonly ISurveyResponseRepository _repository;

    public GetMyLatestSurveyResponseQueryHandler(ISurveyResponseRepository repository)
    {
        _repository = repository;
    }

    public async Task<SurveyResponseDto?> Handle(GetMyLatestSurveyResponseQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetLatestForUserAsync(request.UserId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        JsonElement parsed;
        try
        {
            parsed = JsonDocument.Parse(entity.Answers).RootElement.Clone();
        }
        catch (JsonException)
        {
            parsed = JsonDocument.Parse("{}").RootElement.Clone();
        }

        return new SurveyResponseDto
        {
            Version = entity.Version,
            CompletedAt = entity.CompletedAt,
            Answers = parsed,
        };
    }
}
