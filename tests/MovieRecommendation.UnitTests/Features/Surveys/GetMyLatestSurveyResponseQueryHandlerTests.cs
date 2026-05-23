using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Features.Surveys.Queries.GetMyLatestResponse;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Users;

namespace MovieRecommendation.UnitTests.Features.Surveys;

public class GetMyLatestSurveyResponseQueryHandlerTests
{
    private readonly Mock<ISurveyResponseRepository> _repositoryMock;
    private readonly GetMyLatestSurveyResponseQueryHandler _handler;

    public GetMyLatestSurveyResponseQueryHandlerTests()
    {
        _repositoryMock = new Mock<ISurveyResponseRepository>();
        _handler = new GetMyLatestSurveyResponseQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_NoResponse()
    {
        _repositoryMock
            .Setup(r => r.GetLatestForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SurveyResponse?)null);

        var result = await _handler.Handle(new GetMyLatestSurveyResponseQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ParseAnswers_When_Found()
    {
        var userId = Guid.NewGuid();
        var entity = new SurveyResponse
        {
            UserId = userId,
            Version = 1,
            CompletedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Answers = "{\"mood_preferences\":{\"happy\":4}}",
        };
        _repositoryMock
            .Setup(r => r.GetLatestForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _handler.Handle(new GetMyLatestSurveyResponseQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Version.Should().Be(1);
        result.Answers.GetProperty("mood_preferences").GetProperty("happy").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task Handle_Should_FallBackToEmpty_When_AnswersInvalidJson()
    {
        var userId = Guid.NewGuid();
        var entity = new SurveyResponse
        {
            UserId = userId,
            Version = 1,
            Answers = "not-json",
        };
        _repositoryMock
            .Setup(r => r.GetLatestForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await _handler.Handle(new GetMyLatestSurveyResponseQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Answers.EnumerateObject().Should().BeEmpty();
    }
}
