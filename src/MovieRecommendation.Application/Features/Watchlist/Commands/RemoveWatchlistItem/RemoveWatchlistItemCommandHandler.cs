using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Watchlist.Commands.RemoveWatchlistItem;

public class RemoveWatchlistItemCommandHandler : ICommandHandler<RemoveWatchlistItemCommand>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public RemoveWatchlistItemCommandHandler(
        IWatchlistRepository watchlistRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _watchlistRepository = watchlistRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Unit> Handle(RemoveWatchlistItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _watchlistRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.MovieId,
            cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(
                $"User {request.UserId} has no watchlist entry for movie {request.MovieId}.");
        }

        _watchlistRepository.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(request.UserId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.BecauseForUser(request.UserId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(request.UserId));
        return Unit.Value;
    }
}
