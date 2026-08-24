namespace MovieRecommendation.Application.Interfaces;

public interface ITmdbPersonImporter
{
    Task<TmdbPersonResult?> FetchByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default);
}
