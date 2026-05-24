using MediatR;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Enums;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Watchlist.Commands.UpdateWatchlistStatus;

public class UpdateWatchlistStatusCommandHandler : ICommandHandler<UpdateWatchlistStatusCommand>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IWatchHistoryRepository _watchHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateWatchlistStatusCommandHandler(
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

    public async Task<Unit> Handle(UpdateWatchlistStatusCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var item = await _watchlistRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.MovieId,
            cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(
                $"User {request.UserId} has no watchlist entry for movie {request.MovieId}.");
        }

        item.Status = request.Model.Status;
        if (request.Model.Notes is not null)
        {
            item.Notes = request.Model.Notes;
        }
        item.UpdatedAt = now;

        if (request.Model.Status == WatchlistStatus.Completed)
        {
            await _watchHistoryRepository.UpsertAsync(
                request.UserId,
                request.MovieId,
                now,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(request.UserId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(request.UserId));
        return Unit.Value;
    }
}
