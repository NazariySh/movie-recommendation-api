using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Enums;

namespace MovieRecommendation.Application.Features.Watchlist.Commands.UpsertWatchlistItem;

public class UpsertWatchlistItemCommandHandler : ICommandHandler<UpsertWatchlistItemCommand>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IWatchHistoryRepository _watchHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpsertWatchlistItemCommandHandler(
        IWatchlistRepository watchlistRepository,
        IWatchHistoryRepository watchHistoryRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        IDateTimeProvider dateTimeProvider)
    {
        _watchlistRepository = watchlistRepository;
        _watchHistoryRepository = watchHistoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Unit> Handle(UpsertWatchlistItemCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var existing = await _watchlistRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.Model.MovieId,
            cancellationToken);

        if (existing is null)
        {
            _watchlistRepository.Add(new WatchlistItem
            {
                UserId = request.UserId,
                MovieId = request.Model.MovieId,
                Status = request.Model.Status,
                Notes = request.Model.Notes,
            });
        }
        else
        {
            existing.IsDeleted = false;
            existing.Status = request.Model.Status;
            existing.Notes = request.Model.Notes;
            existing.UpdatedAt = now;
        }

        if (request.Model.Status == WatchlistStatus.Completed)
        {
            await _watchHistoryRepository.UpsertAsync(
                request.UserId,
                request.Model.MovieId,
                now,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(request.UserId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(request.UserId));
        return Unit.Value;
    }
}
