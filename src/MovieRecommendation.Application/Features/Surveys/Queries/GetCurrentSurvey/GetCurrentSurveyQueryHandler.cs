using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Surveys;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.Application.Features.Surveys.Queries.GetCurrentSurvey;

public class GetCurrentSurveyQueryHandler : IQueryHandler<GetCurrentSurveyQuery, SurveyDto>
{
    private const string FallbackLang = "en";

    private readonly IOptionsMonitor<SurveySettings> _options;

    public GetCurrentSurveyQueryHandler(IOptionsMonitor<SurveySettings> options)
    {
        _options = options;
    }

    public Task<SurveyDto> Handle(GetCurrentSurveyQuery request, CancellationToken cancellationToken)
    {
        var schema = _options.CurrentValue;

        var dto = new SurveyDto
        {
            Version = schema.Version,
            Questions = schema.Questions
                .Select(q => new SurveyQuestionDto
                {
                    Id = q.Id,
                    Type = q.Type,
                    Question = Localise(q.Question, request.Lang),
                    Required = q.Required,
                    Rows = q.Rows,
                    Max = q.Max,
                    MinRequired = q.MinRequired,
                    MoviePool = q.MoviePool,
                    Scale = q.Scale is null ? null : new SurveyScaleDto { Min = q.Scale.Min, Max = q.Scale.Max },
                    Options = q.Options?
                        .Select(o => new SurveyOptionDto
                        {
                            Id = o.Id,
                            Label = Localise(o.Label, request.Lang) ?? o.Id,
                        })
                        .ToList(),
                })
                .ToList(),
        };

        return Task.FromResult(dto);
    }

    private static string? Localise(IDictionary<string, string>? translations, string lang)
    {
        if (translations is null || translations.Count == 0)
        {
            return null;
        }

        if (translations.TryGetValue(lang, out var match))
        {
            return match;
        }

        if (translations.TryGetValue(FallbackLang, out var fallback))
        {
            return fallback;
        }

        return translations.Values.FirstOrDefault();
    }
}
