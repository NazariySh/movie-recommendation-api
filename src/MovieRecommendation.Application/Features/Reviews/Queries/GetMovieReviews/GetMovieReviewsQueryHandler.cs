using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Models;

namespace MovieRecommendation.Application.Features.Reviews.Queries.GetMovieReviews;

public class GetMovieReviewsQueryHandler : IQueryHandler<GetMovieReviewsQuery, PagedList<MovieReviewDto>>
{
    private readonly IMovieReviewRepository _reviewRepository;
    private readonly ICacheService _cache;

    public GetMovieReviewsQueryHandler(IMovieReviewRepository reviewRepository, ICacheService cache)
    {
        _reviewRepository = reviewRepository;
        _cache = cache;
    }

    public Task<PagedList<MovieReviewDto>> Handle(GetMovieReviewsQuery request, CancellationToken cancellationToken)
    {
        var key = ReviewCacheKeys.MovieReviewsPage(
            request.MovieId,
            request.ViewerId,
            request.Query.Sort,
            request.Query.PageNumber,
            request.Query.PageSize);

        return _cache.GetOrSetAsync(
            key,
            ct => _reviewRepository.GetForMovieAsync(request.MovieId, request.Query, request.ViewerId, ct),
            TimeSpan.FromMinutes(2),
            cancellationToken);
    }
}
