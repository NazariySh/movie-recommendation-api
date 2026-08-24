namespace MovieRecommendation.Infrastructure.Data.Seeding.Constants;

public static class SeedingConstants
{
    public const int ProgressLogInterval = 100;
    public const int RatingsBatchSize = 1000;
    public const int TranslationBatchSize = 100;
    public const string DefaultDataDirectory = "ml-latest-small";
    public const string MoviesFileName = "movies.csv";
    public const string LinksFileName = "links.csv";
    public const string RatingsFileName = "ratings.csv";
    public const string MovieLensUserEmailDomain = "local.moviematch";
    public const string MovieLensUserPassword = "Movielens123!";
}
