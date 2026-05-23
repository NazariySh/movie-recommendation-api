using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Ratings.Queries.GetUserRatings;

public class GetUserRatingsQueryHandler : IQueryHandler<GetUserRatingsQuery, PagedList<UserRatingDto>>
{
    private readonly IRatingRepository _ratingRepository;

    public GetUserRatingsQueryHandler(IRatingRepository ratingRepository)
    {
        _ratingRepository = ratingRepository;
    }

    public Task<PagedList<UserRatingDto>> Handle(GetUserRatingsQuery request, CancellationToken cancellationToken)
    {
        return _ratingRepository.GetUserRatingsAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            request.Lang,
            cancellationToken);
    }
}
