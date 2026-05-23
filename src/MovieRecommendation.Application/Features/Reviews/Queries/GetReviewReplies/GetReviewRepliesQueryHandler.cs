using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.Application.Features.Reviews.Queries.GetReviewReplies;

public class GetReviewRepliesQueryHandler : IQueryHandler<GetReviewRepliesQuery, IReadOnlyList<MovieReviewDto>>
{
    private readonly IMovieReviewRepository _reviewRepository;

    public GetReviewRepliesQueryHandler(IMovieReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public Task<IReadOnlyList<MovieReviewDto>> Handle(GetReviewRepliesQuery request, CancellationToken cancellationToken)
    {
        return _reviewRepository.GetRepliesAsync(request.ReviewId, request.ViewerId, cancellationToken);
    }
}
