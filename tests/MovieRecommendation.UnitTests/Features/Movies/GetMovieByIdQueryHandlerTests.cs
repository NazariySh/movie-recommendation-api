using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Features.Movies.Queries.GetMovieById;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class GetMovieByIdQueryHandlerTests
{
    private readonly Mock<IMovieRepository> _movieRepositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly GetMovieByIdQueryHandler _handler;

    public GetMovieByIdQueryHandlerTests()
    {
        _movieRepositoryMock = new Mock<IMovieRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler = new GetMovieByIdQueryHandler(_movieRepositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnFromCache_AndUseDetailKey()
    {
        var id = Guid.NewGuid();
        var expectedKey = MovieCacheKeys.Detail(id, "en");
        var dto = new MovieDetailDto { Id = id, Title = "Cached" };

        _cacheMock
            .Setup(c => c.GetOrSetAsync<MovieDetailDto?>(
                expectedKey,
                It.IsAny<Func<CancellationToken, Task<MovieDetailDto?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _handler.Handle(new GetMovieByIdQuery(id, "en"), CancellationToken.None);

        result.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_RepositoryReturnsNull()
    {
        _cacheMock
            .Setup(c => c.GetOrSetAsync<MovieDetailDto?>(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<MovieDetailDto?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((MovieDetailDto?)null);

        var act = () => _handler.Handle(new GetMovieByIdQuery(Guid.NewGuid(), "en"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
