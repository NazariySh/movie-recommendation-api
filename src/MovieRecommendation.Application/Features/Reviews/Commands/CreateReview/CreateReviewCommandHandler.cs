using System.Net;
using AutoMapper;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Common.Constants;
using MovieRecommendation.Application.DTOs.Reviews;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Reviews;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : ICommandHandler<CreateReviewCommand, MovieReviewDto>
{
    private const int DailySubmitLimit = 10;

    private readonly IMovieReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public CreateReviewCommandHandler(
        IMovieReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cache)
    {
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<MovieReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var submittedToday = await _reviewRepository.CountSubmittedTodayAsync(request.UserId, cancellationToken);
        if (submittedToday >= DailySubmitLimit)
        {
            throw new DomainException(
                HttpStatusCode.TooManyRequests,
                $"Review submission limit ({DailySubmitLimit}/day) reached. Try again later.");
        }

        var review = new MovieReview
        {
            UserId = request.UserId,
            MovieId = request.MovieId,
            Body = request.Model.Body,
            IsSpoiler = request.Model.IsSpoiler,
            Score = request.Model.Score,
        };

        _reviewRepository.Add(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.RemoveByPrefix(ReviewCacheKeys.ForMovie(request.MovieId));
        _cache.RemoveByPrefix(UserCacheKeys.StatsForUser(request.UserId));

        var page = await _reviewRepository.GetForMovieAsync(
            request.MovieId,
            new SearchReviewsDto { PageNumber = 1, PageSize = 1, Sort = "newest" },
            request.UserId,
            cancellationToken);

        var fresh = page.Items.FirstOrDefault(r => r.Id == review.Id);
        return fresh ?? _mapper.Map<MovieReviewDto>(review);
    }
}
