using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Movies;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Movies.Queries.GetMovieByImdbId;

public class GetMovieByImdbIdQueryHandler : IQueryHandler<GetMovieByImdbIdQuery, MovieDetailDto>
{
    private readonly IMovieRepository _movieRepository;

    public GetMovieByImdbIdQueryHandler(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<MovieDetailDto> Handle(GetMovieByImdbIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetDetailByImdbIdAsync(
            request.ImdbId,
            request.Lang,
            cancellationToken);

        if (movie is null)
        {
            throw new NotFoundException($"Movie with IMDb id {request.ImdbId} not found");
        }

        return movie;
    }
}
