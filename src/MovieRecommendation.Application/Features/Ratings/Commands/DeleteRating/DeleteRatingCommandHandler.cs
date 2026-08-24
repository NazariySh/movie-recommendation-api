using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Ratings.Commands.DeleteRating;

public class DeleteRatingCommandHandler : ICommandHandler<DeleteRatingCommand>
{
    private readonly IRatingRepository _ratingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteRatingCommandHandler(
        IRatingRepository ratingRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _ratingRepository = ratingRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteRatingCommand request, CancellationToken cancellationToken)
    {
        var rating = await _ratingRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.MovieId,
            cancellationToken);

        if (rating is null)
        {
            throw new NotFoundException(
                $"User {request.UserId} has no rating on movie {request.MovieId}.");
        }

        _ratingRepository.Remove(rating);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _ratingRepository.RecomputeAggregatesAsync(request.MovieId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);
        _cache.RemoveByPrefix(MovieCacheKeys.DetailFor(request.MovieId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(request.UserId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(request.UserId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.BecauseForUser(request.UserId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(request.UserId));

        return Unit.Value;
    }
}
