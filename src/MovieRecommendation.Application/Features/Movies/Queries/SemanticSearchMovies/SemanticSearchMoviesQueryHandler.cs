using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Movies.Queries.SemanticSearchMovies;

public class SemanticSearchMoviesQueryHandler
    : IQueryHandler<SemanticSearchMoviesQuery, SemanticSearchMoviesResult>
{
    private readonly ISearchEngine _searchEngine;
    private readonly IMovieRepository _movieRepository;

    public SemanticSearchMoviesQueryHandler(
        ISearchEngine searchEngine,
        IMovieRepository movieRepository)
    {
        _searchEngine = searchEngine;
        _movieRepository = movieRepository;
    }

    public async Task<SemanticSearchMoviesResult> Handle(
        SemanticSearchMoviesQuery query,
        CancellationToken cancellationToken)
    {
        var trimmed = query.Query?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new SemanticSearchMoviesResult([], query.Query ?? string.Empty, 0);
        }

        var scored = await _searchEngine.SemanticSearchAsync(trimmed, query.Limit, cancellationToken);

        var qualifying = scored
            .Where(s => s.Score >= query.MinScore)
            .ToList();

        if (qualifying.Count == 0)
        {
            return new SemanticSearchMoviesResult([], trimmed, 0);
        }

        var ids = qualifying.Select(s => s.MovieId).ToList();
        var movies = await _movieRepository.GetListItemsByIdsAsync(ids, query.Lang, cancellationToken);

        var scoreById = qualifying.ToDictionary(s => s.MovieId, s => s.Score);
        var orderById = ids
            .Select((id, idx) => (id, idx))
            .ToDictionary(x => x.id, x => x.idx);

        var items = movies
            .OrderBy(m => orderById[m.Id])
            .Select(m => new SemanticSearchMovieItem(
                Id: m.Id,
                Title: m.Title,
                Overview: m.Overview,
                PosterUrl: m.PosterUrl,
                ReleaseYear: m.ReleaseYear,
                VoteAverage: m.AverageRating,
                SimilarityScore: scoreById[m.Id]))
            .ToList();

        return new SemanticSearchMoviesResult(items, trimmed, items.Count);
    }
}
