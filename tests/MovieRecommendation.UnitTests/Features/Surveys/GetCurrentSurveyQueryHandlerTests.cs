using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MovieRecommendation.Application.Features.Surveys.Queries.GetCurrentSurvey;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.UnitTests.Features.Surveys;

public class GetCurrentSurveyQueryHandlerTests
{
    private readonly Mock<IOptionsMonitor<SurveySettings>> _optionsMock;
    private readonly GetCurrentSurveyQueryHandler _handler;

    public GetCurrentSurveyQueryHandlerTests()
    {
        _optionsMock = new Mock<IOptionsMonitor<SurveySettings>>();
        _handler = new GetCurrentSurveyQueryHandler(_optionsMock.Object);
    }

    [Fact]
    public async Task Handle_Should_LocaliseQuestionAndOptionLabels()
    {
        _optionsMock.Setup(o => o.CurrentValue).Returns(new SurveySettings
        {
            Version = 1,
            Questions =
            [
                new SurveyQuestionSettings
                {
                    Id = "mood",
                    Type = "single_select",
                    Question = new Dictionary<string, string> { ["uk"] = "Настрій?", ["en"] = "Mood?" },
                    Required = true,
                    Options =
                    [
                        new SurveyOptionSettings
                        {
                            Id = "happy",
                            Label = new Dictionary<string, string> { ["uk"] = "Щасливий", ["en"] = "Happy" },
                        },
                    ],
                },
            ],
        });

        var result = await _handler.Handle(new GetCurrentSurveyQuery("uk"), CancellationToken.None);

        result.Version.Should().Be(1);
        result.Questions.Single().Question.Should().Be("Настрій?");
        result.Questions.Single().Options!.Single().Label.Should().Be("Щасливий");
    }

    [Fact]
    public async Task Handle_Should_FallBackToEnglish_When_LangMissing()
    {
        _optionsMock.Setup(o => o.CurrentValue).Returns(new SurveySettings
        {
            Version = 1,
            Questions =
            [
                new SurveyQuestionSettings
                {
                    Id = "mood",
                    Type = "single_select",
                    Question = new Dictionary<string, string> { ["en"] = "Mood?" },
                    Required = false,
                    Options =
                    [
                        new SurveyOptionSettings
                        {
                            Id = "happy",
                            Label = new Dictionary<string, string> { ["en"] = "Happy" },
                        },
                    ],
                },
            ],
        });

        var result = await _handler.Handle(new GetCurrentSurveyQuery("fr"), CancellationToken.None);

        result.Questions.Single().Question.Should().Be("Mood?");
        result.Questions.Single().Options!.Single().Label.Should().Be("Happy");
    }

    [Fact]
    public async Task Handle_Should_FallBackToOptionId_When_NoLabel()
    {
        _optionsMock.Setup(o => o.CurrentValue).Returns(new SurveySettings
        {
            Version = 1,
            Questions =
            [
                new SurveyQuestionSettings
                {
                    Id = "mood",
                    Type = "single_select",
                    Question = new Dictionary<string, string>(),
                    Required = false,
                    Options =
                    [
                        new SurveyOptionSettings { Id = "happy", Label = new Dictionary<string, string>() },
                    ],
                },
            ],
        });

        var result = await _handler.Handle(new GetCurrentSurveyQuery("uk"), CancellationToken.None);

        result.Questions.Single().Options!.Single().Label.Should().Be("happy");
    }
}
