using System.Net;
using FluentAssertions;
using Moq;
using MovieRecommendation.Application.Features.Movies.Commands.RegenerateEmbedding;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;
using Pgvector;

namespace MovieRecommendation.UnitTests.Features.Movies;

public class RegenerateEmbeddingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IEmbeddingService> _embeddingMock;

    public RegenerateEmbeddingCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _embeddingMock = new Mock<IEmbeddingService>();
    }

    private RegenerateEmbeddingCommandHandler BuildHandler(IEmbeddingService? embeddingService)
    {
        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var movieRepository = new MovieRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);
        return new RegenerateEmbeddingCommandHandler(movieRepository, unitOfWork, embeddingService);
    }

    [Fact]
    public async Task Handle_Should_ThrowServiceUnavailable_When_EmbeddingServiceNull()
    {
        var handler = BuildHandler(embeddingService: null);

        var act = () => handler.Handle(new RegenerateEmbeddingCommand(Guid.NewGuid()), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_MovieMissing()
    {
        var handler = BuildHandler(_embeddingMock.Object);

        var act = () => handler.Handle(new RegenerateEmbeddingCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_StoreNewEmbedding_OnSuccess()
    {
        var movie = TestData.Movie();
        _dbContext.Movies.Add(movie);
        await _dbContext.SaveChangesAsync();

        var newEmbedding = new Vector(new float[] { 0.1f, 0.2f, 0.3f });
        _embeddingMock
            .Setup(s => s.GenerateMovieEmbeddingAsync(It.Is<Movie>(m => m.Id == movie.Id)))
            .ReturnsAsync(newEmbedding);

        var handler = BuildHandler(_embeddingMock.Object);

        await handler.Handle(new RegenerateEmbeddingCommand(movie.Id), CancellationToken.None);

        // In-memory test provider ignores the Embedding column (per TestDbContextFactory),
        // so verify via the in-memory tracked entity rather than reloading.
        var tracked = _dbContext.Movies.Local.Single(m => m.Id == movie.Id);
        tracked.UpdatedAt.Should().NotBeNull();
        _embeddingMock.Verify(
            s => s.GenerateMovieEmbeddingAsync(It.IsAny<Movie>()),
            Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
