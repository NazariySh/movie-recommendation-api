using AutoMapper;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Abstractions.Time;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Ratings;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;

namespace MovieRecommendation.Application.Features.Ratings.Commands.UpsertRating;

public class UpsertRatingCommandHandler : ICommandHandler<UpsertRatingCommand, RatingDto>
{
    private const decimal AutoWatchHistoryThreshold = 7m;

    private readonly IRatingRepository _ratingRepository;
    private readonly IWatchHistoryRepository _watchHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpsertRatingCommandHandler(
        IRatingRepository ratingRepository,
        IWatchHistoryRepository watchHistoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cache,
        IDateTimeProvider dateTimeProvider)
    {
        _ratingRepository = ratingRepository;
        _watchHistoryRepository = watchHistoryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RatingDto> Handle(UpsertRatingCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var existing = await _ratingRepository.GetByUserAndMovieAsync(
            request.UserId,
            request.MovieId,
            cancellationToken);

        MovieRating rating;

        if (existing is null)
        {
            rating = new MovieRating
            {
                UserId = request.UserId,
                MovieId = request.MovieId,
                Score = request.Model.Value,
            };
            _ratingRepository.Add(rating);
        }
        else
        {
            existing.Score = request.Model.Value;
            existing.UpdatedAt = now;
            rating = existing;
        }

        if (request.Model.Value >= AutoWatchHistoryThreshold)
        {
            await _watchHistoryRepository.UpsertAsync(
                request.UserId,
                request.MovieId,
                now,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _ratingRepository.RecomputeAggregatesAsync(request.MovieId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        InvalidateCaches(request.UserId, request.MovieId);

        return _mapper.Map<RatingDto>(rating);
    }

    private void InvalidateCaches(Guid userId, Guid movieId)
    {
        _cache.RemoveByPrefix(MovieCacheKeys.ListPrefix);
        _cache.RemoveByPrefix(MovieCacheKeys.DetailFor(movieId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ForYouFor(userId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.ColdStartFor(userId));
        _cache.RemoveByPrefix(RecommendationCacheKeys.BecauseForUser(userId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(userId));
    }
}
