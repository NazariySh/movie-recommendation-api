using FluentAssertions;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Common.Models;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.ML.Recommenders;
using MovieRecommendation.UnitTests.Infrastructure;

namespace MovieRecommendation.UnitTests.ML;

public class ColdStartRecommenderTests
{
    [Fact]
    public async Task Recommends_TopRated_When_NoGenresProvided()
    {
        using var db = TestDbContextFactory.Create();

        // Many votes, average 8.5 → Bayesian: (1000*8.5 + 10*7) / 1010 = 8.485
        var blockbuster = TestData.Movie(title: "Blockbuster", key: "block");
        blockbuster.AverageRating = 8.5m;
        blockbuster.RatingsCount = 1000;

        // One vote of 10 → Bayesian: (1*10 + 10*7) / 11 = 7.27 — should rank below blockbuster
        var luckyOne = TestData.Movie(title: "Lucky", key: "lucky");
        luckyOne.AverageRating = 10m;
        luckyOne.RatingsCount = 1;

        // Average movie
        var midPack = TestData.Movie(title: "MidPack", key: "midpack");
        midPack.AverageRating = 7.0m;
        midPack.RatingsCount = 200;

        db.Movies.AddRange(blockbuster, luckyOne, midPack);
        await db.SaveChangesAsync();

        var sut = BuildSut(db);

        var result = await sut.GetColdStartRecommendationsAsync(genreIds: [], limit: 10);

        result.Should().HaveCount(3);
        result[0].MovieId.Should().Be(blockbuster.Id);
        result[1].MovieId.Should().Be(luckyOne.Id);
        result[2].MovieId.Should().Be(midPack.Id);
        result.Should().AllSatisfy(m => m.Reason.Should().Be(RecommendationReason.TopRated));
    }

    [Fact]
    public async Task Filters_By_GenreIds_When_Provided()
    {
        using var db = TestDbContextFactory.Create();

        var drama = TestData.Movie(title: "Drama", key: "drama");
        drama.AverageRating = 8m;
        drama.RatingsCount = 100;
        drama.MovieGenres = [new MovieGenre { GenreId = 18 }];

        var comedy = TestData.Movie(title: "Comedy", key: "comedy");
        comedy.AverageRating = 9m;
        comedy.RatingsCount = 100;
        comedy.MovieGenres = [new MovieGenre { GenreId = 35 }];

        db.Movies.AddRange(drama, comedy);
        await db.SaveChangesAsync();

        var sut = BuildSut(db);

        var result = await sut.GetColdStartRecommendationsAsync(genreIds: [18], limit: 10);

        result.Should().HaveCount(1);
        result[0].MovieId.Should().Be(drama.Id);
        result[0].Reason.Should().Be(RecommendationReason.PopularInGenres);
    }

    [Fact]
    public async Task BayesianAverage_PullsLowVoteHighScore_BackTowardGlobalMean()
    {
        using var db = TestDbContextFactory.Create();

        // 5 votes, average 10 → Bayesian: (5*10 + 10*7) / 15 = 8.0
        var lowVoteHigh = TestData.Movie(title: "LowVoteHigh", key: "low");
        lowVoteHigh.AverageRating = 10m;
        lowVoteHigh.RatingsCount = 5;

        // 500 votes, average 8.4 → Bayesian: (500*8.4 + 10*7) / 510 = 8.3725
        var highVoteHigh = TestData.Movie(title: "HighVoteHigh", key: "high");
        highVoteHigh.AverageRating = 8.4m;
        highVoteHigh.RatingsCount = 500;

        db.Movies.AddRange(lowVoteHigh, highVoteHigh);
        await db.SaveChangesAsync();

        var sut = BuildSut(db);

        var result = await sut.GetColdStartRecommendationsAsync(genreIds: [], limit: 10);

        result.Should().HaveCount(2);
        result[0].MovieId.Should().Be(highVoteHigh.Id);
        result[1].MovieId.Should().Be(lowVoteHigh.Id);
    }

    private static ColdStartRecommender BuildSut(MovieRecommendation.Infrastructure.Data.ApplicationDbContext db)
    {
        var settings = Options.Create(new RecommendationSettings
        {
            BayesianMinVotes = 10,
            BayesianGlobalMean = 7.0,
        });
        return new ColdStartRecommender(db, settings);
    }
}
