using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetMovieByKey;

public class GetMovieByKeyQueryHandler : IQueryHandler<GetMovieByKeyQuery, MovieDetailDto>
{
    private readonly IMovieRepository _movieRepository;

    public GetMovieByKeyQueryHandler(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<MovieDetailDto> Handle(GetMovieByKeyQuery request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetDetailByKeyAsync(
            request.MovieKey,
            request.Lang,
            request.CurrentUserId,
            cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with key {request.MovieKey} not found");
        }

        return movie;
    }
}
