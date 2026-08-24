using AutoMapper;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Ratings.Queries.GetMyRating;

public class GetMyRatingQueryHandler : IQueryHandler<GetMyRatingQuery, RatingDto?>
{
    private readonly IRatingRepository _ratingRepository;
    private readonly IMapper _mapper;

    public GetMyRatingQueryHandler(IRatingRepository ratingRepository, IMapper mapper)
    {
        _ratingRepository = ratingRepository;
        _mapper = mapper;
    }

    public async Task<RatingDto?> Handle(GetMyRatingQuery request, CancellationToken cancellationToken)
    {
        var rating = await _ratingRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.MovieId,
            cancellationToken);

        return rating is null ? null : _mapper.Map<RatingDto>(rating);
    }
}
