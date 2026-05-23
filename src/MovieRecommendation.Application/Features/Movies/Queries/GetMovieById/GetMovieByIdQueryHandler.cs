using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetMovieById;

public class GetMovieByIdQueryHandler : IQueryHandler<GetMovieByIdQuery, MovieDetailDto>
{
    private readonly IMovieRepository _movieRepository;
    private readonly ICacheService _cache;

    public GetMovieByIdQueryHandler(IMovieRepository movieRepository, ICacheService cache)
    {
        _movieRepository = movieRepository;
        _cache = cache;
    }

    public async Task<MovieDetailDto> Handle(GetMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var key = MovieCacheKeys.Detail(request.Id, request.Lang, request.CurrentUserId);
        var movie = await _cache.GetOrSetAsync(
            key,
            ct => _movieRepository.GetDetailByIdAsync(request.Id, request.Lang, request.CurrentUserId, ct),
            TimeSpan.FromMinutes(30),
            cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with ID {request.Id} not found");
        }

        return movie;
    }
}
