using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Reviews.Commands.ToggleHelpful;

public class ToggleHelpfulCommandHandler : ICommandHandler<ToggleHelpfulCommand, ToggleHelpfulResult>
{
    private readonly IMovieReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ToggleHelpfulCommandHandler(
        IMovieReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IDateTimeProvider dateTimeProvider)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ToggleHelpfulResult> Handle(ToggleHelpfulCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetTrackedAsync(request.ReviewId, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException($"Review {request.ReviewId} not found.");
        }

        if (review.UserId == request.UserId)
        {
            throw new ForbiddenException("Cannot mark your own review as helpful.");
        }

        var existing = await _reviewRepository.HasHelpfulVoteAsync(
            request.UserId,
            request.ReviewId,
            cancellationToken);

        bool nowMarked;

        if (existing)
        {
            await _reviewRepository.RemoveHelpfulVoteAsync(
                request.UserId,
                request.ReviewId,
                cancellationToken);

            review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
            nowMarked = false;
        }
        else
        {
            _reviewRepository.AddHelpfulVote(request.UserId, request.ReviewId);
            review.HelpfulCount += 1;
            nowMarked = true;
        }

        review.UpdatedAt = _dateTimeProvider.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(ReviewCacheKeys.ForMovie(review.MovieId));

        return new ToggleHelpfulResult(review.HelpfulCount, nowMarked);
    }
}
