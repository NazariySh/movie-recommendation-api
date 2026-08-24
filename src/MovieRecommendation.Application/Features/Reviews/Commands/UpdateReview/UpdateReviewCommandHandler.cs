using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandler : ICommandHandler<UpdateReviewCommand>
{
    private readonly IMovieReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateReviewCommandHandler(
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

    public async Task<Unit> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetTrackedAsync(request.Id, cancellationToken);

        if (review is null)
        {
            throw new NotFoundException($"Review {request.Id} not found.");
        }

        if (review.UserId != request.UserId)
        {
            throw new ForbiddenException("Only the author can edit this review.");
        }

        review.Body = request.Model.Body;
        review.IsSpoiler = request.Model.IsSpoiler;
        review.Score = request.Model.Score;
        review.UpdatedAt = _dateTimeProvider.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(ReviewCacheKeys.ForMovie(review.MovieId));

        return Unit.Value;
    }
}
