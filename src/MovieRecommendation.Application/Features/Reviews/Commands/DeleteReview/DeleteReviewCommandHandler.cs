using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandHandler : ICommandHandler<DeleteReviewCommand>
{
    private readonly IMovieReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteReviewCommandHandler(
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

    public async Task<Unit> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetTrackedAsync(request.Id, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException($"Review {request.Id} not found.");
        }

        if (!request.IsModerator && review.UserId != request.UserId)
        {
            throw new ForbiddenException("Only the author or a moderator can delete this review.");
        }

        var now = _dateTimeProvider.UtcNow;
        review.IsDeleted = true;
        review.UpdatedAt = now;

        foreach (var reply in review.Replies)
        {
            reply.IsDeleted = true;
            reply.UpdatedAt = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(ReviewCacheKeys.ForMovie(review.MovieId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(review.UserId));

        return Unit.Value;
    }
}
