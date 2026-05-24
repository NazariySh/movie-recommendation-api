using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Features.Reviews.Commands.CreateReply;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Domain.Exceptions;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Reviews;

public class CreateReplyCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly CreateReplyCommandHandler _handler;

    public CreateReplyCommandHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _cacheMock = new Mock<ICacheService>();

        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var reviewRepository = new MovieReviewRepository(_dbContext, mapperConfig);
        var unitOfWork = new UnitOfWork(_dbContext);

        _handler = new CreateReplyCommandHandler(
            reviewRepository,
            unitOfWork,
            TestMapperFactory.Create(),
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_ParentReviewMissing()
    {
        var command = new CreateReplyCommand(
            ParentReviewId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Model: new CreateReplyDto { Body = "Nice." });

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowBadRequest_When_ParentIsItselfAReply()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        var topLevelId = Guid.NewGuid();
        var midReplyId = Guid.NewGuid();

        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var topLevel = TestData.Review(userId, movieId);
        topLevel.Id = topLevelId;
        var midReply = TestData.Review(userId, movieId, parentReviewId: topLevelId);
        midReply.Id = midReplyId;
        _dbContext.MovieReviews.Add(topLevel);
        _dbContext.MovieReviews.Add(midReply);
        await _dbContext.SaveChangesAsync();

        var act = () => _handler.Handle(
            new CreateReplyCommand(midReplyId, userId, new CreateReplyDto { Body = "Reply to a reply" }),
            CancellationToken.None);

        (await act.Should().ThrowAsync<DomainException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Handle_Should_CreateReplyWithNullScore_And_InvalidateCache()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId));
        var parent = TestData.Review(userId, movieId);
        _dbContext.MovieReviews.Add(parent);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateReplyDto
        {
            Body = "Thanks for the recommendation!",
            IsSpoiler = false,
            Score = 9m,
        };

        var result = await _handler.Handle(
            new CreateReplyCommand(parent.Id, userId, dto),
            CancellationToken.None);

        var savedReply = await _dbContext.MovieReviews
            .SingleAsync(r => r.ParentReviewId == parent.Id);
        savedReply.Score.Should().BeNull();
        savedReply.Body.Should().Be(dto.Body);
        savedReply.MovieId.Should().Be(movieId);
        result.Id.Should().Be(savedReply.Id);

        _cacheMock.Verify(c => c.RemoveByPrefix(ReviewCacheKeys.ForMovie(movieId)), Times.Once);
    }

    public void Dispose() => _dbContext.Dispose();
}
