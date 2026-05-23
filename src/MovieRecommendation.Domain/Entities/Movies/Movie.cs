using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Enums;
using Pgvector;

namespace MovieRecommendation.Domain.Entities.Movies;

public class Movie : BaseEntity, ISoftDeletable
{
    public string Key { get; set; } = null!;

    public int? TmdbId { get; set; }

    public string? ImdbId { get; set; }

    public string OriginalTitle { get; set; } = null!;

    public string OriginalLang { get; set; }

    public TitleType Type { get; set; } = TitleType.Movie;

    public string? PosterUrl { get; set; }

    public string? BackdropUrl { get; set; }

    public string? TrailerYoutubeId { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public MovieStatus Status { get; set; }

    public int? Runtime { get; set; }

    public bool? IsOngoing { get; set; }

    public Vector? Embedding { get; set; }

    public decimal AverageRating { get; set; }

    public int RatingsCount { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<MovieTranslation> Translations { get; set; } = [];

    public ICollection<MovieGenre> MovieGenres { get; set; } = [];

    public ICollection<MovieKeyword> Keywords { get; set; } = [];

    public ICollection<Season> Seasons { get; set; } = [];

    public ICollection<MovieCast> MovieCasts { get; set; } = [];

    public ICollection<MovieRating> Ratings { get; set; } = [];

    public ICollection<MovieReview> Reviews { get; set; } = [];

    public ICollection<WatchHistory> WatchHistory { get; set; } = [];
}
