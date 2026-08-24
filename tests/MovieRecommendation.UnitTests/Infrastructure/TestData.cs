using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Entities.Users;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.UnitTests.Infrastructure;

public static class TestData
{
    public static User User(
        Guid? id = null,
        string userName = "tester",
        string email = "tester@example.com")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PreferredLanguage = "en",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    public static Movie Movie(
        Guid? id = null,
        string title = "Test Movie",
        string key = "test-movie",
        TitleType type = TitleType.Movie)
    {
        return new Movie
        {
            Id = id ?? Guid.NewGuid(),
            Key = key,
            OriginalTitle = title,
            OriginalLang = "en",
            Type = type,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    public static MovieTranslation Translation(
        Guid movieId,
        string title,
        string lang = "en",
        string? overview = null)
    {
        return new MovieTranslation
        {
            MovieId = movieId,
            LanguageCode = lang,
            Title = title,
            Overview = overview,
        };
    }

    public static WatchlistItem WatchlistItem(
        Guid userId,
        Guid movieId,
        WatchlistStatus status = WatchlistStatus.PlanToWatch,
        string? notes = null,
        DateTime? createdAt = null)
    {
        return new WatchlistItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MovieId = movieId,
            Status = status,
            Notes = notes,
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    public static MovieRating Rating(
        Guid userId,
        Guid movieId,
        decimal score = 7m,
        DateTime? createdAt = null)
    {
        return new MovieRating
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MovieId = movieId,
            Score = score,
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    public static Person Person(
        Guid? id = null,
        string name = "Jane Doe",
        string slug = "jane-doe",
        string? imdbId = null,
        bool isDeleted = false)
    {
        return new Person
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Slug = slug,
            ImdbId = imdbId,
            IsDeleted = isDeleted,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    public static MovieCast Cast(
        Guid movieId,
        Guid personId,
        string role = "acting",
        string? character = null,
        int? castOrder = null)
    {
        return new MovieCast
        {
            MovieId = movieId,
            PersonId = personId,
            Role = role,
            Character = character,
            CastOrder = castOrder,
        };
    }

    public static MovieReview Review(
        Guid userId,
        Guid movieId,
        string body = "A solid film.",
        decimal? score = 8m,
        bool isSpoiler = false,
        int helpfulCount = 0,
        Guid? parentReviewId = null,
        DateTime? createdAt = null)
    {
        return new MovieReview
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MovieId = movieId,
            ParentReviewId = parentReviewId,
            Body = body,
            Score = score,
            IsSpoiler = isSpoiler,
            HelpfulCount = helpfulCount,
            CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }
}
