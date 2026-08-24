using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Genres;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Genres.Queries.GetAllGenres;

public class GetAllGenresQueryHandler : IQueryHandler<GetAllGenresQuery, IReadOnlyList<GenreDto>>
{
    private readonly IGenreRepository _genreRepository;
    private readonly ICacheService _cache;

    public GetAllGenresQueryHandler(IGenreRepository genreRepository, ICacheService cache)
    {
        _genreRepository = genreRepository;
        _cache = cache;
    }

    public Task<IReadOnlyList<GenreDto>> Handle(GetAllGenresQuery request, CancellationToken cancellationToken)
    {
        return _cache.GetOrSetAsync(
            GenreCacheKeys.All(request.Lang),
            ct => _genreRepository.GetAllAsync(request.Lang, ct),
            TimeSpan.FromHours(1),
            cancellationToken);
    }
}
