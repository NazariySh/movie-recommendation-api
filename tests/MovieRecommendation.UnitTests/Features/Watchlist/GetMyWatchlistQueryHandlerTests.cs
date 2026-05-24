using FluentAssertions;
using MovieRecommendation.Application.DTOs.Watchlist;
using MovieRecommendation.Application.Features.Watchlist.Queries.GetMyWatchlist;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Infrastructure.Data;
using MovieRecommendation.Infrastructure.Repositories;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.Features.Watchlist;

public class GetMyWatchlistQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetMyWatchlistQueryHandler _handler;

    public GetMyWatchlistQueryHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        var mapperConfig = TestMapperFactory.CreateConfiguration();
        var watchlistRepository = new WatchlistRepository(_dbContext, mapperConfig);
        _handler = new GetMyWatchlistQueryHandler(watchlistRepository);
    }

    [Fact]
    public async Task Handle_Should_ReturnOnlyItemsBelongingToCurrentUser_When_OtherUsersHaveWatchlists()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var movieA = Guid.NewGuid();
        var movieB = Guid.NewGuid();
        _dbContext.Users.AddRange(TestData.User(id: userId), TestData.User(id: otherUserId, userName: "other"));
        _dbContext.Movies.AddRange(TestData.Movie(id: movieA, key: "a"), TestData.Movie(id: movieB, key: "b"));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(userId, movieA));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(otherUserId, movieB));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetMyWatchlistQuery(userId, new SearchWatchlistDto(), "en"),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.MovieId == movieA);
    }

    [Fact]
    public async Task Handle_Should_FilterByStatus_When_StatusFilterIsProvided()
    {
        var userId = Guid.NewGuid();
        var watchingMovieId = Guid.NewGuid();
        var completedMovieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.AddRange(
            TestData.Movie(id: watchingMovieId, key: "watching"),
            TestData.Movie(id: completedMovieId, key: "completed"));
        _dbContext.WatchlistItems.Add(
            TestData.WatchlistItem(userId, watchingMovieId, status: WatchlistStatus.Watching));
        _dbContext.WatchlistItems.Add(
            TestData.WatchlistItem(userId, completedMovieId, status: WatchlistStatus.Completed));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetMyWatchlistQuery(
                userId,
                new SearchWatchlistDto { Status = WatchlistStatus.Completed },
                "en"),
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items.Single().MovieId.Should().Be(completedMovieId);
    }

    [Fact]
    public async Task Handle_Should_ResolveTitleFromTranslationInRequestedLanguage_When_TranslationExists()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId, title: "Fallback Original"));
        _dbContext.MovieTranslations.Add(TestData.Translation(movieId, "Назва українською", lang: "uk"));
        _dbContext.MovieTranslations.Add(TestData.Translation(movieId, "English Title", lang: "en"));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(userId, movieId));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetMyWatchlistQuery(userId, new SearchWatchlistDto(), "uk"),
            CancellationToken.None);

        result.Items.Single().MovieTitle.Should().Be("Назва українською");
    }

    [Fact]
    public async Task Handle_Should_FallbackToEnglishTranslation_When_RequestedLanguageMissing()
    {
        var userId = Guid.NewGuid();
        var movieId = Guid.NewGuid();
        _dbContext.Users.Add(TestData.User(id: userId));
        _dbContext.Movies.Add(TestData.Movie(id: movieId, title: "Fallback Original"));
        _dbContext.MovieTranslations.Add(TestData.Translation(movieId, "English Title", lang: "en"));
        _dbContext.WatchlistItems.Add(TestData.WatchlistItem(userId, movieId));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new GetMyWatchlistQuery(userId, new SearchWatchlistDto(), "uk"),
            CancellationToken.None);

        result.Items.Single().MovieTitle.Should().Be("English Title");
    }

    public void Dispose() => _dbContext.Dispose();
}
