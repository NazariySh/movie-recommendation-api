namespace MovieRecommendation.Application.Interfaces;

public interface IExternalMovieDataProvider
{
    Task<ExternalMovieResult?> FetchByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);
}
